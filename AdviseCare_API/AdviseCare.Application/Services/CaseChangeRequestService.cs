using AdviseCare.Application.Contracts.Requests.CaseChangeRequest;
using AdviseCare.Application.Contracts.Responses.CaseChangeRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Case change requests (EX6): the student of a case, or its current advisor, asks for another advisor for that one case;
    // the Programme Head approves (CASES.advisorId = proposed advisor, written to the stage history) or rejects with decision notes
    public class CaseChangeRequestService : ICaseChangeRequestService
    {
        private readonly IGenericRepository<Casechangerequest> _caseChangeRequestRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Casestagehistory> _caseStageHistoryRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public CaseChangeRequestService(
            IGenericRepository<Casechangerequest> caseChangeRequestRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Casestagehistory> caseStageHistoryRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Referral> referralRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _caseChangeRequestRepository = caseChangeRequestRepository;
            _caseRepository = caseRepository;
            _caseStageHistoryRepository = caseStageHistoryRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _referralRepository = referralRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Case change requests the caller may see, newest first (the Programme Head sees every request;
        // others the requests they made, are proposed for, or that concern their own cases)
        public async Task<IEnumerable<GetAllCaseChangeRequestResponse>> GetAll(Guid CallerId)
        {
            var requests = await GetVisibleRequestsAsync(CallerId, r => true);

            return requests.Select(r => new GetAllCaseChangeRequestResponse
            {
                CaseChangeRequestId = r.CaseChangeRequestId,
                CaseId = r.CaseId,
                CurrentAdvisorId = r.Case.AdvisorId,
                ProgrammeHeadId = r.ProgrammeHeadId,
                ProposedAdvisorId = r.ProposedAdvisorId,
                RequestedBy = r.RequestedBy,
                StudentId = r.Case.StudentId,
                CaseCurrentStage = r.Case.CurrentStage,
                CategoryName = r.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = r.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = r.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = r.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = r.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                DecidedAt = r.DecidedAt,
                RequestedAt = r.RequestedAt,
                Status = r.Status
            });
        }

        // Get all Case change requests the caller may see with pagination
        public async Task<IEnumerable<GetAllCaseChangeRequestWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = await GetVisibleRequestsAsync(CallerId, r => true);

            return requests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllCaseChangeRequestWithPaginationResponse
                {
                    CaseChangeRequestId = r.CaseChangeRequestId,
                    CaseId = r.CaseId,
                    CurrentAdvisorId = r.Case.AdvisorId,
                    ProgrammeHeadId = r.ProgrammeHeadId,
                    ProposedAdvisorId = r.ProposedAdvisorId,
                    RequestedBy = r.RequestedBy,
                    StudentId = r.Case.StudentId,
                    CaseCurrentStage = r.Case.CurrentStage,
                    CategoryName = r.Case.SupportCategory.CategoryName,
                    CurrentAdvisorFullName = r.Case.Advisor?.FullName ?? string.Empty,
                    DecisionNotes = r.DecisionNotes ?? string.Empty,
                    ProgrammeHeadFullName = r.ProgrammeHead?.FullName ?? string.Empty,
                    ProposedAdvisorFullName = r.ProposedAdvisor?.FullName ?? string.Empty,
                    Reason = r.Reason,
                    RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                    StudentFullName = r.Case.Student.FullName,
                    SupportType = r.Case.SupportCategory.SupportType,
                    DecidedAt = r.DecidedAt,
                    RequestedAt = r.RequestedAt,
                    Status = r.Status
                });
        }

        // Get Case change request by Id (the Programme Head, or the people involved)
        public async Task<GetCaseChangeRequestByIdResponse> GetByIdAsync(Guid CallerId, Guid CaseChangeRequestId)
        {
            var request = (await GetVisibleRequestsAsync(CallerId, r => r.CaseChangeRequestId == CaseChangeRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Case change request not found.");

            return new GetCaseChangeRequestByIdResponse
            {
                CaseChangeRequestId = request.CaseChangeRequestId,
                CaseId = request.CaseId,
                CurrentAdvisorId = request.Case.AdvisorId,
                ProgrammeHeadId = request.ProgrammeHeadId,
                ProposedAdvisorId = request.ProposedAdvisorId,
                RequestedBy = request.RequestedBy,
                StudentId = request.Case.StudentId,
                CaseCurrentStage = request.Case.CurrentStage,
                CategoryName = request.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = request.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = request.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = request.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = request.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = request.Reason,
                RequestedByFullName = request.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = request.Case.Student.FullName,
                SupportType = request.Case.SupportCategory.SupportType,
                DecidedAt = request.DecidedAt,
                RequestedAt = request.RequestedAt,
                Status = request.Status
            };
        }

        // Search Case change requests the caller may see by case and status
        public async Task<List<SearchCaseChangeRequestResponse>> SearchAsync(Guid CallerId, SearchCaseChangeRequestRequest request)
        {
            var requests = await GetVisibleRequestsAsync(CallerId, r =>
                (request.CaseId == null || r.CaseId == request.CaseId) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            return requests.Select(r => new SearchCaseChangeRequestResponse
            {
                CaseChangeRequestId = r.CaseChangeRequestId,
                CaseId = r.CaseId,
                CurrentAdvisorId = r.Case.AdvisorId,
                ProgrammeHeadId = r.ProgrammeHeadId,
                ProposedAdvisorId = r.ProposedAdvisorId,
                RequestedBy = r.RequestedBy,
                StudentId = r.Case.StudentId,
                CaseCurrentStage = r.Case.CurrentStage,
                CategoryName = r.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = r.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = r.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = r.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = r.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = r.Reason,
                RequestedByFullName = r.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                DecidedAt = r.DecidedAt,
                RequestedAt = r.RequestedAt,
                Status = r.Status
            }).ToList();
        }

        // Create Case change request: the case's student or current advisor proposes an active advisor of the case's type;
        // one pending request per case; the Programme Heads are notified
        public async Task<CreateCaseChangeRequestResponse> CreateAsync(Guid CreatorId, CreateCaseChangeRequestRequest request)
        {
            var caseRecord = await GetCaseAsync(request.CaseId);
            if (caseRecord.StudentId != CreatorId && caseRecord.AdvisorId != CreatorId)
                throw new NotFoundException("Case not found.");
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (caseRecord.AdvisorId == null)
                throw new ConflictException("The case has no advisor yet; the Programme Head will match it.");
            if (request.ProposedAdvisorId == caseRecord.AdvisorId)
                throw new ValidationException("The proposed advisor already handles this case.");
            if (await _caseChangeRequestRepository.ExistsAsync(r => r.CaseId == caseRecord.CaseId && r.Status == "pending"))
                throw new ConflictException("A change request for this case is already waiting for the Programme Head.");

            var proposedAdvisor = await GetActiveAdvisorAsync(request.ProposedAdvisorId, AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");

            var caseChangeRequest = new Casechangerequest
            {
                CaseChangeRequestId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                ProgrammeHeadId = null,
                ProposedAdvisorId = proposedAdvisor.AccountId,
                RequestedBy = CreatorId,
                Reason = reason,
                RequestedAt = DateTime.UtcNow,
                Status = "pending"
            };
            await _caseChangeRequestRepository.CreateAsync(caseChangeRequest);

            var requester = await _accountRepository.GetByIdAsync(CreatorId);
            await _activityLogService.LogAsync(CreatorId, "caseChangeRequested",
                $"Asked to move the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to {proposedAdvisor.FullName}: {reason}");
            foreach (var programmeHead in await GetActiveAccountsInRoleAsync("Programme Head"))
            {
                await _notificationService.NotifyAsync(programmeHead.AccountId, "Case advisor change requested",
                    $"{requester?.FullName} asked to move the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} from {caseRecord.Advisor?.FullName} to {proposedAdvisor.FullName}.",
                    $"/case-change-requests/{caseChangeRequest.CaseChangeRequestId}");
            }

            var created = (await GetRequestsAsync(r => r.CaseChangeRequestId == caseChangeRequest.CaseChangeRequestId)).First();

            return new CreateCaseChangeRequestResponse
            {
                CaseChangeRequestId = created.CaseChangeRequestId,
                CaseId = created.CaseId,
                CurrentAdvisorId = created.Case.AdvisorId,
                ProgrammeHeadId = created.ProgrammeHeadId,
                ProposedAdvisorId = created.ProposedAdvisorId,
                RequestedBy = created.RequestedBy,
                StudentId = created.Case.StudentId,
                CaseCurrentStage = created.Case.CurrentStage,
                CategoryName = created.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = created.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = created.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = created.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = created.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = created.Reason,
                RequestedByFullName = created.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = created.Case.Student.FullName,
                SupportType = created.Case.SupportCategory.SupportType,
                DecidedAt = created.DecidedAt,
                RequestedAt = created.RequestedAt,
                Status = created.Status
            };
        }

        // Approve Case change request: the case moves to the proposed advisor (stage kept, hand-over written to the stage history),
        // with optional decision notes; the student and both advisors are notified. Refused while a referral of the case is pending
        public async Task<ApproveCaseChangeRequestResponse> ApproveAsync(Guid ProgrammeHeadId, Guid CaseChangeRequestId, ApproveCaseChangeRequestRequest request)
        {
            var caseChangeRequest = await GetPendingAsync(CaseChangeRequestId);
            var caseRecord = await GetCaseAsync(caseChangeRequest.CaseId);
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case was closed after the request. Reject the request instead.");
            if (caseRecord.AdvisorId == caseChangeRequest.ProposedAdvisorId)
                throw new ConflictException("The proposed advisor already handles this case. Reject the request instead.");
            if (await _referralRepository.ExistsAsync(r => r.CaseId == caseRecord.CaseId && r.Status == "pending"))
                throw new ConflictException("A referral of this case is waiting for an answer. Decide the request after the referral is accepted or returned.");

            var newAdvisor = await GetActiveAdvisorAsync(caseChangeRequest.ProposedAdvisorId, AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
            var oldAdvisor = caseRecord.Advisor;

            var now = DateTime.UtcNow;
            caseChangeRequest.ProgrammeHeadId = ProgrammeHeadId;
            caseChangeRequest.DecisionNotes = string.IsNullOrWhiteSpace(request.DecisionNotes) ? null : request.DecisionNotes.Trim();
            caseChangeRequest.DecidedAt = now;
            caseChangeRequest.Status = "approved";
            _caseChangeRequestRepository.PrepareUpdate(caseChangeRequest);

            // The request, the case's new advisor and the stage history row are saved together
            caseRecord.AdvisorId = newAdvisor.AccountId;
            caseRecord.Advisor = newAdvisor;
            _caseStageHistoryRepository.PrepareCreate(new Casestagehistory
            {
                CaseStageHistoryId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                ChangedBy = ProgrammeHeadId,
                Reason = $"Advisor changed from {oldAdvisor?.FullName ?? "no advisor"} to {newAdvisor.FullName}: case change request approved ({caseChangeRequest.Reason})",
                ChangedAt = now,
                FromStage = caseRecord.CurrentStage,
                ToStage = caseRecord.CurrentStage
            });
            _caseRepository.PrepareUpdate(caseRecord);
            await _caseChangeRequestRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(ProgrammeHeadId, "caseChangeDecided",
                $"Approved moving the {categoryName} case of {caseRecord.Student.FullName} from {oldAdvisor?.FullName} to {newAdvisor.FullName}");
            await _notificationService.NotifyAsync(caseRecord.StudentId, "Your case has a new advisor",
                $"{newAdvisor.FullName} ({newAdvisor.Email}) now handles your {categoryName} case. Please contact them to arrange a meeting.",
                $"/cases/{caseRecord.CaseId}");
            await _notificationService.NotifyAsync(newAdvisor.AccountId, "New case assigned",
                $"You now handle the {categoryName} case of {caseRecord.Student.FullName} ({caseRecord.Student.Email}), moved from {oldAdvisor?.FullName}.",
                $"/cases/{caseRecord.CaseId}");
            if (oldAdvisor != null)
            {
                await _notificationService.NotifyAsync(oldAdvisor.AccountId, "Case change approved",
                    $"The {categoryName} case of {caseRecord.Student.FullName} moved to {newAdvisor.FullName}.",
                    $"/case-change-requests/{CaseChangeRequestId}");
            }

            var approved = (await GetRequestsAsync(r => r.CaseChangeRequestId == CaseChangeRequestId)).First();

            return new ApproveCaseChangeRequestResponse
            {
                CaseChangeRequestId = approved.CaseChangeRequestId,
                CaseId = approved.CaseId,
                CurrentAdvisorId = approved.Case.AdvisorId,
                ProgrammeHeadId = approved.ProgrammeHeadId,
                ProposedAdvisorId = approved.ProposedAdvisorId,
                RequestedBy = approved.RequestedBy,
                StudentId = approved.Case.StudentId,
                CaseCurrentStage = approved.Case.CurrentStage,
                CategoryName = approved.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = approved.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = approved.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = approved.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = approved.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = approved.Reason,
                RequestedByFullName = approved.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = approved.Case.Student.FullName,
                SupportType = approved.Case.SupportCategory.SupportType,
                DecidedAt = approved.DecidedAt,
                RequestedAt = approved.RequestedAt,
                Status = approved.Status
            };
        }

        // Reject Case change request with decision notes (required); the case keeps its advisor, and the requester,
        // the student and both advisors are told why
        public async Task<RejectCaseChangeRequestResponse> RejectAsync(Guid ProgrammeHeadId, Guid CaseChangeRequestId, RejectCaseChangeRequestRequest request)
        {
            var caseChangeRequest = await GetPendingAsync(CaseChangeRequestId);
            var decisionNotes = request.DecisionNotes.Trim();
            if (decisionNotes.Length == 0)
                throw new ValidationException("Decision notes are required when rejecting.");

            caseChangeRequest.ProgrammeHeadId = ProgrammeHeadId;
            caseChangeRequest.DecisionNotes = decisionNotes;
            caseChangeRequest.DecidedAt = DateTime.UtcNow;
            caseChangeRequest.Status = "rejected";
            _caseChangeRequestRepository.PrepareUpdate(caseChangeRequest);
            await _caseChangeRequestRepository.SaveAsync();

            var caseRecord = await GetCaseAsync(caseChangeRequest.CaseId);
            var proposedAdvisor = await _accountRepository.GetByIdAsync(caseChangeRequest.ProposedAdvisorId);
            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(ProgrammeHeadId, "caseChangeDecided",
                $"Rejected moving the {categoryName} case of {caseRecord.Student.FullName} to {proposedAdvisor?.FullName}: {decisionNotes}");

            var recipients = new List<Guid> { caseChangeRequest.RequestedBy, caseRecord.StudentId, caseChangeRequest.ProposedAdvisorId };
            if (caseRecord.AdvisorId != null)
                recipients.Add(caseRecord.AdvisorId.Value);
            foreach (var recipient in recipients.Distinct())
            {
                await _notificationService.NotifyAsync(recipient, "Case change rejected",
                    $"The request to move the {categoryName} case of {caseRecord.Student.FullName} to {proposedAdvisor?.FullName} was rejected: {decisionNotes}",
                    $"/case-change-requests/{CaseChangeRequestId}");
            }

            var rejected = (await GetRequestsAsync(r => r.CaseChangeRequestId == CaseChangeRequestId)).First();

            return new RejectCaseChangeRequestResponse
            {
                CaseChangeRequestId = rejected.CaseChangeRequestId,
                CaseId = rejected.CaseId,
                CurrentAdvisorId = rejected.Case.AdvisorId,
                ProgrammeHeadId = rejected.ProgrammeHeadId,
                ProposedAdvisorId = rejected.ProposedAdvisorId,
                RequestedBy = rejected.RequestedBy,
                StudentId = rejected.Case.StudentId,
                CaseCurrentStage = rejected.Case.CurrentStage,
                CategoryName = rejected.Case.SupportCategory.CategoryName,
                CurrentAdvisorFullName = rejected.Case.Advisor?.FullName ?? string.Empty,
                DecisionNotes = rejected.DecisionNotes ?? string.Empty,
                ProgrammeHeadFullName = rejected.ProgrammeHead?.FullName ?? string.Empty,
                ProposedAdvisorFullName = rejected.ProposedAdvisor?.FullName ?? string.Empty,
                Reason = rejected.Reason,
                RequestedByFullName = rejected.RequestedByNavigation?.FullName ?? string.Empty,
                StudentFullName = rejected.Case.Student.FullName,
                SupportType = rejected.Case.SupportCategory.SupportType,
                DecidedAt = rejected.DecidedAt,
                RequestedAt = rejected.RequestedAt,
                Status = rejected.Status
            };
        }

        // Helper: load Case change requests with their case (student, advisor, category), the proposed advisor, the requester and the Programme Head, newest first
        private async Task<List<Casechangerequest>> GetRequestsAsync(Expression<Func<Casechangerequest, bool>> Predicate)
        {
            return await _caseChangeRequestRepository.SearchAsyncIncludeOrderBy(Predicate, r => r.RequestedAt, false,
                r => r.Case.Student, r => r.Case.Advisor, r => r.Case.SupportCategory, r => r.ProposedAdvisor, r => r.RequestedByNavigation, r => r.ProgrammeHead);
        }

        // Helper: the requests the caller may see: all for the Programme Head; otherwise the ones they made, are proposed for, or that concern their own cases
        private async Task<List<Casechangerequest>> GetVisibleRequestsAsync(Guid CallerId, Expression<Func<Casechangerequest, bool>> Predicate)
        {
            var isProgrammeHead = await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == CallerId && ar.Role.RoleName == "Programme Head");
            var requests = await GetRequestsAsync(Predicate);
            return requests.Where(r => isProgrammeHead ||
                                       r.RequestedBy == CallerId ||
                                       r.ProposedAdvisorId == CallerId ||
                                       r.Case.AdvisorId == CallerId ||
                                       r.Case.StudentId == CallerId).ToList();
        }

        // Helper: a request that is still pending
        private async Task<Casechangerequest> GetPendingAsync(Guid CaseChangeRequestId)
        {
            var caseChangeRequest = await _caseChangeRequestRepository.GetByIdAsync(CaseChangeRequestId)
                ?? throw new NotFoundException("Case change request not found.");
            if (caseChangeRequest.Status != "pending")
                throw new ConflictException($"The request was already {caseChangeRequest.Status}.");
            return caseChangeRequest;
        }

        // Helper: load one Case with its student, advisor and category
        private async Task<Case> GetCaseAsync(Guid CaseId)
        {
            return await _caseRepository.GetByIdAsyncInclude(CaseId, c => c.Student, c => c.Advisor, c => c.SupportCategory)
                ?? throw new NotFoundException("Case not found.");
        }

        // Helper: the active accounts that have a role, by name
        private async Task<List<Account>> GetActiveAccountsInRoleAsync(string RoleName)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.Role.RoleName == RoleName && ar.Account.Status == "active", ar => ar.Account))
                .Select(ar => ar.Account)
                .ToList();
        }

        // Helper: an active account with the given advisor role
        private async Task<Account> GetActiveAdvisorAsync(Guid AdvisorId, string RoleName)
        {
            var advisor = await _accountRepository.GetByIdAsync(AdvisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            if (advisor.Status != "active")
                throw new ValidationException($"{advisor.FullName} is not an active account.");
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == AdvisorId && ar.Role.RoleName == RoleName))
                throw new ValidationException($"{advisor.FullName} does not have the role {RoleName}.");
            return advisor;
        }

        // Helper: which advisor role handles which support type
        private static string AdvisorRoleFor(string SupportType)
        {
            return SupportType == "wellbeing" ? "Wellbeing Advisor" : "Academic Advisor";
        }
    }
}
