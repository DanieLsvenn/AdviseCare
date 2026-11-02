using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
using AdviseCare.Application.Contracts.Responses.CaseChangeRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D5 / EX6: the student of a case, or its current advisor, asks for another advisor for that one case;
    // the Programme Head approves (CASES.advisorId = proposed advisor, written to the stage history) or rejects with a reason (decisionNotes).
    public class CaseChangeRequestService(
        IGenericRepository<Casechangerequest> requestRepository,
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Account> accountRepository,
        INotificationService notificationService,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : ICaseChangeRequestService
    {
        private static readonly string[] RequestStatuses = { "pending", "approved", "rejected" };

        private readonly IGenericRepository<Casechangerequest> _requestRepository = requestRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        public async Task<CaseChangeRequestResponse> CreateAsync(Guid requestedBy, CreateCaseChangeRequestRequest request)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(request.CaseId);
            if (caseRecord.StudentId != requestedBy && caseRecord.AdvisorId != requestedBy)
                throw new NotFoundException("Case not found.");
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId == null)
                throw new ValidationException("The case has no advisor yet; the Programme Head will match it.");
            if (request.ProposedAdvisorId == caseRecord.AdvisorId)
                throw new ValidationException("The proposed advisor already handles this case.");
            if (await _requestRepository.ExistsAsync(r => r.CaseId == caseRecord.CaseId && r.Status == CaseRules.Pending))
                throw new ConflictException("A change request for this case is already waiting for the Programme Head.");

            // The proposed advisor must handle the case's support type, as when the case was matched
            var (proposedAdvisor, _) = await _workflow.GetActiveAdvisorAsync(
                request.ProposedAdvisorId, CaseRules.AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");

            var changeRequest = new Casechangerequest
            {
                CaseChangeRequestId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                // Set to the Programme Head who decides
                ProgrammeHeadId = null,
                ProposedAdvisorId = proposedAdvisor.AccountId,
                RequestedBy = requestedBy,
                Reason = reason,
                RequestedAt = DateTime.UtcNow,
                Status = CaseRules.Pending
            };
            await _requestRepository.CreateAsync(changeRequest);

            var requester = await _accountRepository.GetByIdAsync(requestedBy);
            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(requestedBy, "caseChangeRequested",
                $"Asked to move the {categoryName} case of {caseRecord.Student.FullName} to {proposedAdvisor.FullName}: {reason}");
            foreach (var programmeHead in await _workflow.GetActiveAccountsInRoleAsync(CaseRules.ProgrammeHeadRole))
            {
                await _notificationService.NotifyAsync(programmeHead.AccountId,
                    "Case advisor change requested",
                    $"{requester?.FullName} asked to move the {categoryName} case of {caseRecord.Student.FullName} from {caseRecord.Advisor?.FullName} to {proposedAdvisor.FullName}.",
                    CaseRules.ChangeRequestUrl(changeRequest.CaseChangeRequestId));
            }

            return await GetByIdAsync(requestedBy, changeRequest.CaseChangeRequestId);
        }

        public async Task<List<CaseChangeRequestResponse>> SearchAsync(Guid callerId, SearchCaseChangeRequestRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, RequestStatuses, "Status");
            var isProgrammeHead = (await _workflow.GetRoleNamesAsync(callerId)).Contains(CaseRules.ProgrammeHeadRole);

            // The Programme Head sees every request; others only the requests they made, are proposed for, or that concern their own cases
            var requests = await _requestRepository.SearchAsyncIncludeOrderBy(
                r => (isProgrammeHead ||
                      r.RequestedBy == callerId ||
                      r.ProposedAdvisorId == callerId ||
                      r.Case.AdvisorId == callerId ||
                      r.Case.StudentId == callerId) &&
                     (!request.CaseId.HasValue || r.CaseId == request.CaseId.Value) &&
                     (status == null || r.Status == status),
                r => r.RequestedAt,
                false,
                r => r.Case);
            return await ToResponsesAsync(requests);
        }

        public async Task<CaseChangeRequestResponse> GetByIdAsync(Guid callerId, Guid caseChangeRequestId)
        {
            var changeRequest = await _requestRepository.GetByIdAsyncInclude(caseChangeRequestId, r => r.Case)
                ?? throw new NotFoundException("Case change request not found.");
            var involved = changeRequest.RequestedBy == callerId ||
                           changeRequest.ProposedAdvisorId == callerId ||
                           changeRequest.Case.AdvisorId == callerId ||
                           changeRequest.Case.StudentId == callerId;
            if (!involved && !(await _workflow.GetRoleNamesAsync(callerId)).Contains(CaseRules.ProgrammeHeadRole))
                throw new NotFoundException("Case change request not found.");

            return (await ToResponsesAsync(new List<Casechangerequest> { changeRequest }))[0];
        }

        public async Task<CaseChangeRequestResponse> ApproveAsync(Guid programmeHeadId, Guid caseChangeRequestId, ApproveCaseChangeRequestRequest request)
        {
            var changeRequest = await GetPendingAsync(caseChangeRequestId);
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(changeRequest.CaseId);
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case was closed after the request. Reject the request instead.");
            if (caseRecord.AdvisorId == changeRequest.ProposedAdvisorId)
                throw new ConflictException("The proposed advisor already handles this case. Reject the request instead.");
            if (await _workflow.HasPendingReferralAsync(caseRecord.CaseId))
                throw new ConflictException("A referral of this case is waiting for an answer. Decide the request after the referral is accepted or returned.");

            var (newAdvisor, _) = await _workflow.GetActiveAdvisorAsync(
                changeRequest.ProposedAdvisorId, CaseRules.AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var oldAdvisor = caseRecord.Advisor;
            var notes = string.IsNullOrWhiteSpace(request?.DecisionNotes) ? null : request!.DecisionNotes!.Trim();

            var now = DateTime.UtcNow;
            changeRequest.Status = "approved";
            changeRequest.DecidedAt = now;
            changeRequest.ProgrammeHeadId = programmeHeadId;
            changeRequest.DecisionNotes = notes;
            _requestRepository.PrepareUpdate(changeRequest);
            // The request, the case's new advisor and the stage history row are saved together
            _workflow.StageAdvisorChange(caseRecord, newAdvisor, programmeHeadId, $"Case change request approved: {changeRequest.Reason}", now);
            await _requestRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(programmeHeadId, "caseChangeDecided",
                $"Approved moving the {categoryName} case of {caseRecord.Student.FullName} from {oldAdvisor?.FullName} to {newAdvisor.FullName}");

            // EX6 step 4: both advisors and the student are notified (the requester is one of them)
            var url = CaseRules.ChangeRequestUrl(changeRequest.CaseChangeRequestId);
            await _notificationService.NotifyAsync(caseRecord.StudentId,
                "Your case has a new advisor",
                $"{newAdvisor.FullName} ({newAdvisor.Email}) now handles your {categoryName} case. Please contact them to arrange a meeting.",
                CaseRules.CaseUrl(caseRecord.CaseId));
            await _notificationService.NotifyAsync(newAdvisor.AccountId,
                "New case assigned",
                $"You now handle the {categoryName} case of {caseRecord.Student.FullName} ({caseRecord.Student.Email}), moved from {oldAdvisor?.FullName}.",
                CaseRules.CaseUrl(caseRecord.CaseId));
            if (oldAdvisor != null)
            {
                await _notificationService.NotifyAsync(oldAdvisor.AccountId,
                    "Case change approved",
                    $"The {categoryName} case of {caseRecord.Student.FullName} moved to {newAdvisor.FullName}.",
                    url);
            }

            return await GetByIdAsync(programmeHeadId, caseChangeRequestId);
        }

        public async Task<CaseChangeRequestResponse> RejectAsync(Guid programmeHeadId, Guid caseChangeRequestId, RejectCaseChangeRequestRequest request)
        {
            var changeRequest = await GetPendingAsync(caseChangeRequestId);
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");

            changeRequest.Status = "rejected";
            changeRequest.DecidedAt = DateTime.UtcNow;
            changeRequest.ProgrammeHeadId = programmeHeadId;
            changeRequest.DecisionNotes = reason;
            _requestRepository.PrepareUpdate(changeRequest);
            await _requestRepository.SaveAsync();

            var caseRecord = await _workflow.GetCaseWithDetailsAsync(changeRequest.CaseId);
            var proposed = await _accountRepository.GetByIdAsync(changeRequest.ProposedAdvisorId);
            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(programmeHeadId, "caseChangeDecided",
                $"Rejected moving the {categoryName} case of {caseRecord.Student.FullName} to {proposed?.FullName}: {reason}");

            // EX6 step 4: both advisors and the student are told, with the reason
            var recipients = new List<Guid> { changeRequest.RequestedBy, caseRecord.StudentId, changeRequest.ProposedAdvisorId };
            if (caseRecord.AdvisorId.HasValue) recipients.Add(caseRecord.AdvisorId.Value);
            foreach (var recipient in recipients.Distinct())
            {
                await _notificationService.NotifyAsync(recipient,
                    "Case change rejected",
                    $"The request to move the {categoryName} case of {caseRecord.Student.FullName} to {proposed?.FullName} was rejected: {reason}",
                    CaseRules.ChangeRequestUrl(changeRequest.CaseChangeRequestId));
            }

            return await GetByIdAsync(programmeHeadId, caseChangeRequestId);
        }

        private async Task<Casechangerequest> GetPendingAsync(Guid caseChangeRequestId)
        {
            var changeRequest = await _requestRepository.GetByIdAsync(caseChangeRequestId)
                ?? throw new NotFoundException("Case change request not found.");
            if (changeRequest.Status != CaseRules.Pending)
                throw new ConflictException($"The request was already {changeRequest.Status}.");
            return changeRequest;
        }

        private async Task<List<CaseChangeRequestResponse>> ToResponsesAsync(List<Casechangerequest> requests)
        {
            if (requests.Count == 0) return new List<CaseChangeRequestResponse>();

            var caseIds = requests.Select(r => r.CaseId).Distinct().ToList();
            var cases = (await _caseRepository.SearchAsyncInclude(c => caseIds.Contains(c.CaseId), c => c.SupportCategory))
                .ToDictionary(c => c.CaseId);
            var accountIds = requests
                .SelectMany(r => new[] { r.ProposedAdvisorId, r.RequestedBy, r.ProgrammeHeadId ?? Guid.Empty })
                .Concat(cases.Values.SelectMany(c => new[] { c.StudentId, c.AdvisorId ?? Guid.Empty }))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            var names = (await _accountRepository.SearchAsync(a => accountIds.Contains(a.AccountId)))
                .ToDictionary(a => a.AccountId, a => a.FullName);
            string Name(Guid? id) => id.HasValue ? names.GetValueOrDefault(id.Value) ?? string.Empty : string.Empty;

            return requests.Select(r =>
            {
                var caseRecord = cases.GetValueOrDefault(r.CaseId);
                return new CaseChangeRequestResponse
                {
                    CaseChangeRequestId = r.CaseChangeRequestId,
                    CaseId = r.CaseId,
                    CategoryName = caseRecord?.SupportCategory?.CategoryName ?? string.Empty,
                    SupportType = caseRecord?.SupportCategory?.SupportType ?? string.Empty,
                    CaseCurrentStage = caseRecord?.CurrentStage ?? string.Empty,
                    StudentId = caseRecord?.StudentId ?? Guid.Empty,
                    StudentName = Name(caseRecord?.StudentId),
                    CurrentAdvisorId = caseRecord?.AdvisorId,
                    CurrentAdvisorName = Name(caseRecord?.AdvisorId),
                    ProposedAdvisorId = r.ProposedAdvisorId,
                    ProposedAdvisorName = Name(r.ProposedAdvisorId),
                    RequestedBy = r.RequestedBy,
                    RequestedByName = Name(r.RequestedBy),
                    ProgrammeHeadId = r.ProgrammeHeadId,
                    ProgrammeHeadName = Name(r.ProgrammeHeadId),
                    Reason = r.Reason,
                    DecisionNotes = r.DecisionNotes ?? string.Empty,
                    RequestedAt = CaseRules.AsUtc(r.RequestedAt),
                    DecidedAt = CaseRules.AsUtc(r.DecidedAt),
                    Status = r.Status
                };
            }).ToList();
        }
    }
}
