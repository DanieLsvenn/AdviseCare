using AdviseCare.Application.Contracts.Requests.AssignmentChangeRequest;
using AdviseCare.Application.Contracts.Responses.AssignmentChangeRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D5 / EX6: an advisor (or the student) asks for a different advisor; the Programme Head approves or rejects.
    // ASSIGNMENTCHANGEREQUESTS has no column for the rejection reason, so it is kept in the activity log and sent in the notifications.
    public class AssignmentChangeRequestService(
        IGenericRepository<Assignmentchangerequest> requestRepository,
        IGenericRepository<Advisorassignment> assignmentRepository,
        IGenericRepository<Account> accountRepository,
        INotificationService notificationService,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : IAssignmentChangeRequestService
    {
        private static readonly string[] RequestStatuses = { "pending", "approved", "rejected" };

        private readonly IGenericRepository<Assignmentchangerequest> _requestRepository = requestRepository;
        private readonly IGenericRepository<Advisorassignment> _assignmentRepository = assignmentRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        public async Task<AssignmentChangeRequestResponse> CreateAsync(Guid requestedBy, CreateAssignmentChangeRequestRequest request)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(request.AdvisorAssignmentId)
                ?? throw new NotFoundException("Advisor assignment not found.");
            if (assignment.AdvisorId != requestedBy && assignment.StudentId != requestedBy)
                throw new NotFoundException("Advisor assignment not found.");
            if (assignment.Status != CaseRules.Active)
                throw new ConflictException("The assignment is no longer active.");
            if (request.ProposedAdvisorId == assignment.AdvisorId)
                throw new ValidationException("The proposed advisor is already the student's advisor.");
            if (await _requestRepository.ExistsAsync(r =>
                    r.AdvisorAssignmentId == assignment.AdvisorAssignmentId && r.Status == CaseRules.Pending))
                throw new ConflictException("A change request for this assignment is already waiting for the Programme Head.");

            var (proposedAdvisor, _) = await _workflow.GetActiveAdvisorAsync(request.ProposedAdvisorId, CaseRules.AdvisorRoles);
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var programmeHeads = await _workflow.GetActiveAccountsInRoleAsync(CaseRules.ProgrammeHeadRole);

            var changeRequest = new Assignmentchangerequest
            {
                AssignmentChangeRequestId = Guid.NewGuid(),
                AdvisorAssignmentId = assignment.AdvisorAssignmentId,
                // There is one Programme Head for the whole app; null when none is active yet (any Programme Head can still decide)
                ProgrammeHeadId = programmeHeads.FirstOrDefault()?.AccountId,
                ProposedAdvisorId = proposedAdvisor.AccountId,
                RequestedBy = requestedBy,
                Reason = reason,
                RequestedAt = DateTime.UtcNow,
                Status = CaseRules.Pending
            };
            await _requestRepository.CreateAsync(changeRequest);

            var requester = await _accountRepository.GetByIdAsync(requestedBy);
            var student = await _accountRepository.GetByIdAsync(assignment.StudentId);
            await _activityLogService.LogAsync(requestedBy, "assignmentChangeRequested",
                $"Asked to move {student?.FullName} to {proposedAdvisor.FullName}: {reason}");
            foreach (var programmeHead in programmeHeads)
            {
                await _notificationService.NotifyAsync(programmeHead.AccountId,
                    "Advisor change requested",
                    $"{requester?.FullName} asked to move {student?.FullName} to {proposedAdvisor.FullName}.",
                    CaseRules.ChangeRequestUrl(changeRequest.AssignmentChangeRequestId));
            }

            return await GetByIdAsync(requestedBy, changeRequest.AssignmentChangeRequestId);
        }

        public async Task<List<AssignmentChangeRequestResponse>> SearchAsync(Guid callerId, SearchAssignmentChangeRequestRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, RequestStatuses, "Status");
            var isProgrammeHead = (await _workflow.GetRoleNamesAsync(callerId)).Contains(CaseRules.ProgrammeHeadRole);

            var requests = await _requestRepository.SearchAsyncIncludeOrderBy(
                r => (isProgrammeHead ||
                      r.RequestedBy == callerId ||
                      r.ProposedAdvisorId == callerId ||
                      r.AdvisorAssignment.AdvisorId == callerId ||
                      r.AdvisorAssignment.StudentId == callerId) &&
                     (!request.AdvisorAssignmentId.HasValue || r.AdvisorAssignmentId == request.AdvisorAssignmentId.Value) &&
                     (status == null || r.Status == status),
                r => r.RequestedAt,
                false,
                r => r.AdvisorAssignment);
            return await ToResponsesAsync(requests);
        }

        public async Task<AssignmentChangeRequestResponse> GetByIdAsync(Guid callerId, Guid assignmentChangeRequestId)
        {
            var changeRequest = await _requestRepository.GetByIdAsyncInclude(assignmentChangeRequestId, r => r.AdvisorAssignment)
                ?? throw new NotFoundException("Assignment change request not found.");
            var involved = changeRequest.RequestedBy == callerId ||
                           changeRequest.ProposedAdvisorId == callerId ||
                           changeRequest.AdvisorAssignment.AdvisorId == callerId ||
                           changeRequest.AdvisorAssignment.StudentId == callerId;
            if (!involved && !(await _workflow.GetRoleNamesAsync(callerId)).Contains(CaseRules.ProgrammeHeadRole))
                throw new NotFoundException("Assignment change request not found.");

            return (await ToResponsesAsync(new List<Assignmentchangerequest> { changeRequest }))[0];
        }

        public async Task<AssignmentChangeRequestResponse> ApproveAsync(Guid programmeHeadId, Guid assignmentChangeRequestId)
        {
            var changeRequest = await GetPendingAsync(assignmentChangeRequestId);
            var assignment = changeRequest.AdvisorAssignment;
            if (assignment.Status != CaseRules.Active)
                throw new ConflictException("The assignment was already revoked. Reject this request instead.");

            var (newAdvisor, newRoles) = await _workflow.GetActiveAdvisorAsync(changeRequest.ProposedAdvisorId, CaseRules.AdvisorRoles);
            var oldAdvisor = await _accountRepository.GetByIdAsync(assignment.AdvisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            var student = await _accountRepository.GetByIdAsync(assignment.StudentId)
                ?? throw new NotFoundException("Student not found.");

            changeRequest.Status = "approved";
            changeRequest.DecidedAt = DateTime.UtcNow;
            changeRequest.ProgrammeHeadId = programmeHeadId;
            _requestRepository.PrepareUpdate(changeRequest);

            // Saves the decision together with the revoked/new assignments and the moved cases
            var handover = await AdvisorAssignmentService.CommitHandoverAsync(assignment, oldAdvisor, newAdvisor, newRoles, student,
                programmeHeadId, $"Change request approved: {changeRequest.Reason}", _workflow, _assignmentRepository, _notificationService);

            await _activityLogService.LogAsync(programmeHeadId, "assignmentChangeDecided",
                $"Approved moving {student.FullName} from {oldAdvisor.FullName} to {newAdvisor.FullName} ({handover.MovedCaseIds.Count} open case(s) moved)");
            if (changeRequest.RequestedBy != oldAdvisor.AccountId && changeRequest.RequestedBy != student.AccountId)
            {
                await _notificationService.NotifyAsync(changeRequest.RequestedBy,
                    "Advisor change approved",
                    $"Your request to move {student.FullName} to {newAdvisor.FullName} was approved.",
                    CaseRules.ChangeRequestUrl(changeRequest.AssignmentChangeRequestId));
            }

            var response = await GetByIdAsync(programmeHeadId, assignmentChangeRequestId);
            response.Handover = handover;
            return response;
        }

        public async Task<AssignmentChangeRequestResponse> RejectAsync(Guid programmeHeadId, Guid assignmentChangeRequestId, RejectAssignmentChangeRequestRequest request)
        {
            var changeRequest = await GetPendingAsync(assignmentChangeRequestId);
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");

            changeRequest.Status = "rejected";
            changeRequest.DecidedAt = DateTime.UtcNow;
            changeRequest.ProgrammeHeadId = programmeHeadId;
            _requestRepository.PrepareUpdate(changeRequest);
            await _requestRepository.SaveAsync();

            var assignment = changeRequest.AdvisorAssignment;
            var student = await _accountRepository.GetByIdAsync(assignment.StudentId);
            var proposed = await _accountRepository.GetByIdAsync(changeRequest.ProposedAdvisorId);
            await _activityLogService.LogAsync(programmeHeadId, "assignmentChangeDecided",
                $"Rejected moving {student?.FullName} to {proposed?.FullName}: {reason}");

            // EX6 step 4: both advisors and the student are told, with the reason
            var recipients = new[] { changeRequest.RequestedBy, assignment.AdvisorId, assignment.StudentId, changeRequest.ProposedAdvisorId }.Distinct();
            foreach (var recipient in recipients)
            {
                await _notificationService.NotifyAsync(recipient,
                    "Advisor change rejected",
                    $"The request to move {student?.FullName} to {proposed?.FullName} was rejected: {reason}",
                    CaseRules.ChangeRequestUrl(changeRequest.AssignmentChangeRequestId));
            }

            return await GetByIdAsync(programmeHeadId, assignmentChangeRequestId);
        }

        private async Task<Assignmentchangerequest> GetPendingAsync(Guid assignmentChangeRequestId)
        {
            var changeRequest = await _requestRepository.GetByIdAsyncInclude(assignmentChangeRequestId, r => r.AdvisorAssignment)
                ?? throw new NotFoundException("Assignment change request not found.");
            if (changeRequest.Status != CaseRules.Pending)
                throw new ConflictException($"The request was already {changeRequest.Status}.");
            return changeRequest;
        }

        private async Task<List<AssignmentChangeRequestResponse>> ToResponsesAsync(List<Assignmentchangerequest> requests)
        {
            if (requests.Count == 0) return new List<AssignmentChangeRequestResponse>();

            var accountIds = requests
                .SelectMany(r => new[] { r.AdvisorAssignment.AdvisorId, r.AdvisorAssignment.StudentId, r.ProposedAdvisorId, r.RequestedBy, r.ProgrammeHeadId ?? Guid.Empty })
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            var names = (await _accountRepository.SearchAsync(a => accountIds.Contains(a.AccountId)))
                .ToDictionary(a => a.AccountId, a => a.FullName);
            string Name(Guid? id) => id.HasValue ? names.GetValueOrDefault(id.Value) ?? string.Empty : string.Empty;

            return requests.Select(r => new AssignmentChangeRequestResponse
            {
                AssignmentChangeRequestId = r.AssignmentChangeRequestId,
                AdvisorAssignmentId = r.AdvisorAssignmentId,
                CurrentAdvisorId = r.AdvisorAssignment.AdvisorId,
                CurrentAdvisorName = Name(r.AdvisorAssignment.AdvisorId),
                StudentId = r.AdvisorAssignment.StudentId,
                StudentName = Name(r.AdvisorAssignment.StudentId),
                ProposedAdvisorId = r.ProposedAdvisorId,
                ProposedAdvisorName = Name(r.ProposedAdvisorId),
                RequestedBy = r.RequestedBy,
                RequestedByName = Name(r.RequestedBy),
                ProgrammeHeadId = r.ProgrammeHeadId,
                ProgrammeHeadName = Name(r.ProgrammeHeadId),
                Reason = r.Reason,
                RequestedAt = CaseRules.AsUtc(r.RequestedAt),
                DecidedAt = CaseRules.AsUtc(r.DecidedAt),
                Status = r.Status
            }).ToList();
        }
    }
}
