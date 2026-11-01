using AdviseCare.Application.Contracts.Requests.AdvisorAssignment;
using AdviseCare.Application.Contracts.Responses.AdvisorAssignment;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Advisor-student pairs (ADVISORASSIGNMENTS). Rows are created by matching, advisor outreach and accepted referrals;
    // they are never deleted, only revoked (EX6), so the history of who advised whom stays.
    public class AdvisorAssignmentService(
        IGenericRepository<Advisorassignment> assignmentRepository,
        IGenericRepository<Account> accountRepository,
        INotificationService notificationService,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : IAdvisorAssignmentService
    {
        private static readonly string[] AssignmentStatuses = { CaseRules.Active, CaseRules.Revoked };

        private readonly IGenericRepository<Advisorassignment> _assignmentRepository = assignmentRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        public async Task<List<AdvisorAssignmentResponse>> SearchAsync(Guid callerId, SearchAdvisorAssignmentRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, AssignmentStatuses, "Status");
            var roles = await _workflow.GetRoleNamesAsync(callerId);
            var advisorId = request.AdvisorId;
            var studentId = request.StudentId;

            // The Programme Head sees every pair; an advisor only their own pairs; a student only their own advisors
            if (!roles.Contains(CaseRules.ProgrammeHeadRole))
            {
                if (CaseRules.AdvisorRoles.Any(roles.Contains))
                    advisorId = callerId;
                else
                    studentId = callerId;
            }

            var assignments = await _assignmentRepository.SearchAsyncOrderBy(
                a => (!advisorId.HasValue || a.AdvisorId == advisorId.Value) &&
                     (!studentId.HasValue || a.StudentId == studentId.Value) &&
                     (status == null || a.Status == status),
                a => a.AssignedAt,
                false);
            return await _workflow.ToAssignmentResponsesAsync(assignments);
        }

        public async Task<List<AdvisorAssignmentResponse>> GetMyStudentsAsync(Guid advisorId)
        {
            var assignments = await _assignmentRepository.SearchAsync(a => a.AdvisorId == advisorId && a.Status == CaseRules.Active);
            var responses = await _workflow.ToAssignmentResponsesAsync(assignments);
            return responses.OrderBy(r => r.StudentName).ToList();
        }

        public async Task<AdvisorAssignmentResponse> GetByIdAsync(Guid callerId, Guid advisorAssignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(advisorAssignmentId)
                ?? throw new NotFoundException("Advisor assignment not found.");
            if (assignment.AdvisorId != callerId && assignment.StudentId != callerId &&
                !(await _workflow.GetRoleNamesAsync(callerId)).Contains(CaseRules.ProgrammeHeadRole))
                throw new NotFoundException("Advisor assignment not found.");

            return (await _workflow.ToAssignmentResponsesAsync(new List<Advisorassignment> { assignment }))[0];
        }

        // EX6 step 5: the Programme Head moves a student to another advisor directly, without a change request
        public async Task<AdvisorHandoverResponse> ReassignAsync(Guid programmeHeadId, Guid advisorAssignmentId, ReassignAdvisorAssignmentRequest request)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(advisorAssignmentId)
                ?? throw new NotFoundException("Advisor assignment not found.");
            if (assignment.Status != CaseRules.Active)
                throw new ConflictException("Only an active assignment can be reassigned.");
            if (assignment.AdvisorId == request.AdvisorId)
                throw new ValidationException("The student is already assigned to this advisor.");

            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var (newAdvisor, newRoles) = await _workflow.GetActiveAdvisorAsync(request.AdvisorId, CaseRules.AdvisorRoles);
            var oldAdvisor = await _accountRepository.GetByIdAsync(assignment.AdvisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            var student = await _accountRepository.GetByIdAsync(assignment.StudentId)
                ?? throw new NotFoundException("Student not found.");

            var handover = await CommitHandoverAsync(assignment, oldAdvisor, newAdvisor, newRoles, student, programmeHeadId, reason,
                _workflow, _assignmentRepository, _notificationService);

            await _activityLogService.LogAsync(programmeHeadId, "caseReassigned",
                $"Reassigned {student.FullName} from {oldAdvisor.FullName} to {newAdvisor.FullName} ({handover.MovedCaseIds.Count} open case(s) moved): {reason}");
            return handover;
        }

        // Shared by direct reassignment and an approved change request: stage the handover, save once, then notify all three people
        internal static async Task<AdvisorHandoverResponse> CommitHandoverAsync(
            Advisorassignment assignment,
            Account oldAdvisor,
            Account newAdvisor,
            HashSet<string> newRoles,
            Account student,
            Guid actorId,
            string reason,
            CaseWorkflowHelper workflow,
            IGenericRepository<Advisorassignment> assignmentRepository,
            INotificationService notificationService)
        {
            var now = DateTime.UtcNow;
            var (newAssignment, moved, kept) = await workflow.StageHandoverAsync(
                assignment, oldAdvisor, newAdvisor, newRoles, actorId, reason, now);
            await assignmentRepository.SaveAsync();

            await notificationService.NotifyAsync(student.AccountId,
                "You have a new advisor",
                $"{newAdvisor.FullName} ({newAdvisor.Email}) replaces {oldAdvisor.FullName} as your advisor.",
                null);
            await notificationService.NotifyAsync(newAdvisor.AccountId,
                "New student assigned",
                $"{student.FullName} ({student.Email}) is now assigned to you, replacing {oldAdvisor.FullName}. {moved.Count} open case(s) moved to you.",
                null);
            await notificationService.NotifyAsync(oldAdvisor.AccountId,
                "Student moved to another advisor",
                $"{student.FullName} is now assigned to {newAdvisor.FullName}. {moved.Count} open case(s) moved, {kept.Count} stayed with you.",
                null);

            var responses = await workflow.ToAssignmentResponsesAsync(new List<Advisorassignment> { assignment, newAssignment });
            return new AdvisorHandoverResponse
            {
                RevokedAssignment = responses[0],
                NewAssignment = responses[1],
                MovedCaseIds = moved,
                KeptCaseIds = kept
            };
        }
    }
}
