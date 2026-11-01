using AdviseCare.Application.Contracts.Requests.Referral;
using AdviseCare.Application.Contracts.Responses.Referral;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // D4.5: referrals of a case to another advisor (EX4) or to the Welfare Officer (WF2 step 8.2 / WF2b)
    //  - advisor receiver accepts -> becomes the case's advisor, stage referred -> assigned
    //  - Welfare Officer accepts  -> the advisor keeps the case (it comes back for follow-up after the intervention), stage stays referred
    //  - receiver returns         -> stage goes back to what it was before the referral, with the reason in the stage history
    public class ReferralService(
        IGenericRepository<Referral> referralRepository,
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Casestagehistory> stageHistoryRepository,
        IGenericRepository<Account> accountRepository,
        INotificationService notificationService,
        IActivityLogService activityLogService,
        CaseWorkflowHelper workflow) : IReferralService
    {
        private static readonly string[] ReferralStatuses = { "pending", "accepted", "returned" };

        private readonly IGenericRepository<Referral> _referralRepository = referralRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Casestagehistory> _stageHistoryRepository = stageHistoryRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly CaseWorkflowHelper _workflow = workflow;

        public async Task<ReferralResponse> CreateAsync(Guid fromAccountId, CreateReferralRequest request)
        {
            var caseRecord = await _workflow.GetCaseWithDetailsAsync(request.CaseId);
            await _workflow.EnsureCanViewCaseAsync(caseRecord, fromAccountId, await _workflow.GetRoleNamesAsync(fromAccountId));
            if (caseRecord.AdvisorId != fromAccountId)
                throw new ForbiddenException("Only the case's current advisor can refer it.");
            if (caseRecord.Status == CaseRules.StatusClosed)
                throw new ConflictException("The case is closed.");
            if (await _workflow.HasPendingReferralAsync(caseRecord.CaseId))
                throw new ConflictException("This case already has a referral waiting for an answer.");
            if (request.ToAccountId == fromAccountId)
                throw new ValidationException("You cannot refer a case to yourself.");

            var (receiver, receiverRoles) = await _workflow.GetActiveAdvisorAsync(request.ToAccountId,
                CaseRules.AcademicAdvisorRole, CaseRules.WellbeingAdvisorRole, CaseRules.WelfareOfficerRole);
            var reason = CaseWorkflowHelper.RequireText(request.ReferralReason, "Referral reason is required.");
            var priority = CaseWorkflowHelper.OptionalFilter(request.Priority, CaseRules.Priorities, "Priority") ?? "medium";

            var now = DateTime.UtcNow;
            var referral = new Referral
            {
                ReferralId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                FromAccountId = fromAccountId,
                ToAccountId = receiver.AccountId,
                ReferralReason = reason,
                CreatedAt = now,
                Priority = priority,
                Status = CaseRules.Pending
            };
            _referralRepository.PrepareCreate(referral);
            _workflow.StageStageChange(caseRecord, fromAccountId, CaseRules.StageReferred, $"Referred to {receiver.FullName}: {reason}", now);
            _caseRepository.PrepareUpdate(caseRecord);
            await _referralRepository.SaveAsync();

            await _activityLogService.LogAsync(fromAccountId, "referralSent",
                $"Referred the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to {receiver.FullName} ({priority})");
            await _notificationService.NotifyAsync(receiver.AccountId,
                "New referral",
                $"{caseRecord.Advisor?.FullName} referred the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to you ({priority} priority).",
                CaseRules.ReferralUrl(referral.ReferralId));

            return await GetByIdAsync(fromAccountId, referral.ReferralId);
        }

        public async Task<List<ReferralResponse>> GetIncomingAsync(Guid accountId, SearchMyReferralsRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, ReferralStatuses, "Status");
            return await LoadAsync(r => r.ToAccountId == accountId && (status == null || r.Status == status));
        }

        public async Task<List<ReferralResponse>> GetOutgoingAsync(Guid accountId, SearchMyReferralsRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, ReferralStatuses, "Status");
            return await LoadAsync(r => r.FromAccountId == accountId && (status == null || r.Status == status));
        }

        public async Task<List<ReferralResponse>> SearchAsync(Guid callerId, SearchReferralRequest request)
        {
            var status = CaseWorkflowHelper.OptionalFilter(request.Status, ReferralStatuses, "Status");
            var roles = await _workflow.GetRoleNamesAsync(callerId);

            if (request.CaseId.HasValue)
            {
                var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId.Value)
                    ?? throw new NotFoundException("Case not found.");
                if (!await _workflow.CanViewCaseAsStaffAsync(caseRecord, callerId, roles))
                    throw new NotFoundException("Case not found.");
                return await LoadAsync(r => r.CaseId == request.CaseId.Value && (status == null || r.Status == status));
            }

            if (!roles.Contains(CaseRules.ProgrammeHeadRole))
                throw new ValidationException("caseId is required. Use /me/incoming or /me/outgoing for your own referrals.");
            return await LoadAsync(r => status == null || r.Status == status);
        }

        public async Task<ReferralResponse> GetByIdAsync(Guid callerId, Guid referralId)
        {
            var referral = await _referralRepository.GetByIdAsyncInclude(referralId, r => r.Case)
                ?? throw new NotFoundException("Referral not found.");
            if (referral.FromAccountId != callerId && referral.ToAccountId != callerId &&
                !await _workflow.CanViewCaseAsStaffAsync(referral.Case, callerId, await _workflow.GetRoleNamesAsync(callerId)))
                throw new NotFoundException("Referral not found.");

            return (await LoadAsync(r => r.ReferralId == referralId))[0];
        }

        public async Task<ReferralResponse> AcceptAsync(Guid receiverId, Guid referralId)
        {
            var (referral, caseRecord) = await GetPendingForReceiverAsync(receiverId, referralId);
            var receiverRoles = await _workflow.GetRoleNamesAsync(receiverId);
            var receiver = await _accountRepository.GetByIdAsync(receiverId)
                ?? throw new NotFoundException("Account not found.");
            var sender = await _accountRepository.GetByIdAsync(referral.FromAccountId);
            var isWelfareHandover = receiverRoles.Contains(CaseRules.WelfareOfficerRole) &&
                                    !CaseRules.AdvisorRoles.Any(receiverRoles.Contains);

            var now = DateTime.UtcNow;
            referral.Status = "accepted";
            referral.AcceptedAt = now;
            _referralRepository.PrepareUpdate(referral);

            if (!isWelfareHandover)
            {
                // The receiving advisor becomes the handling advisor
                await _workflow.StageEnsureAssignmentAsync(receiverId, caseRecord.StudentId, referral.FromAccountId, now);
                caseRecord.AdvisorId = receiverId;
                caseRecord.Advisor = receiver;
                _workflow.StageStageChange(caseRecord, receiverId, CaseRules.StageAssigned,
                    $"Referral accepted by {receiver.FullName}: {referral.ReferralReason}", now);
                _caseRepository.PrepareUpdate(caseRecord);
            }
            await _referralRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(receiverId, "referralAnswered",
                $"Accepted the referral of the {categoryName} case of {caseRecord.Student.FullName} from {sender?.FullName}");
            await _notificationService.NotifyAsync(referral.FromAccountId,
                "Referral accepted",
                isWelfareHandover
                    ? $"{receiver.FullName} accepted your referral of the {categoryName} case of {caseRecord.Student.FullName}. The case comes back to you for follow-up once the welfare work is done."
                    : $"{receiver.FullName} accepted your referral of the {categoryName} case of {caseRecord.Student.FullName} and is now handling it.",
                CaseRules.ReferralUrl(referral.ReferralId));
            if (!isWelfareHandover)
            {
                await _notificationService.NotifyAsync(caseRecord.StudentId,
                    "Your case has a new advisor",
                    $"{receiver.FullName} ({receiver.Email}) is now handling your {categoryName} case. Please contact them to arrange a meeting.",
                    CaseRules.CaseUrl(caseRecord.CaseId));
            }

            return await GetByIdAsync(receiverId, referralId);
        }

        public async Task<ReferralResponse> ReturnAsync(Guid receiverId, Guid referralId, ReturnReferralRequest request)
        {
            var (referral, caseRecord) = await GetPendingForReceiverAsync(receiverId, referralId);
            var reason = CaseWorkflowHelper.RequireText(request.Reason, "Reason is required.");
            var receiver = await _accountRepository.GetByIdAsync(receiverId)
                ?? throw new NotFoundException("Account not found.");

            // Back to the stage the case had right before it was referred
            var referredRow = (await _stageHistoryRepository.SearchAsyncOrderBy(
                    h => h.CaseId == caseRecord.CaseId && h.ToStage == CaseRules.StageReferred,
                    h => h.ChangedAt,
                    false))
                .FirstOrDefault();
            var previousStage = referredRow == null || referredRow.FromStage == CaseRules.StageReferred
                ? CaseRules.StageAssigned
                : referredRow.FromStage;

            var now = DateTime.UtcNow;
            referral.Status = "returned";
            _referralRepository.PrepareUpdate(referral);
            if (caseRecord.CurrentStage == CaseRules.StageReferred)
            {
                _workflow.StageStageChange(caseRecord, receiverId, previousStage, $"Referral returned by {receiver.FullName}: {reason}", now);
                _caseRepository.PrepareUpdate(caseRecord);
            }
            await _referralRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(receiverId, "referralAnswered",
                $"Returned the referral of the {categoryName} case of {caseRecord.Student.FullName}: {reason}");
            await _notificationService.NotifyAsync(referral.FromAccountId,
                "Referral returned",
                $"{receiver.FullName} returned your referral of the {categoryName} case of {caseRecord.Student.FullName}: {reason}",
                CaseRules.CaseUrl(caseRecord.CaseId));

            return await GetByIdAsync(receiverId, referralId);
        }

        private async Task<(Referral Referral, Case Case)> GetPendingForReceiverAsync(Guid receiverId, Guid referralId)
        {
            var referral = await _referralRepository.GetByIdAsync(referralId)
                ?? throw new NotFoundException("Referral not found.");
            if (referral.ToAccountId != receiverId)
            {
                if (referral.FromAccountId == receiverId)
                    throw new ForbiddenException("Only the receiver can answer a referral.");
                throw new NotFoundException("Referral not found.");
            }
            if (referral.Status != CaseRules.Pending)
                throw new ConflictException($"The referral was already {referral.Status}.");

            var caseRecord = await _workflow.GetCaseWithDetailsAsync(referral.CaseId);
            return (referral, caseRecord);
        }

        private async Task<List<ReferralResponse>> LoadAsync(System.Linq.Expressions.Expression<Func<Referral, bool>> predicate)
        {
            var referrals = await _referralRepository.SearchAsyncIncludeOrderBy(
                predicate, r => r.CreatedAt, false, r => r.Case, r => r.FromAccount, r => r.ToAccount);
            if (referrals.Count == 0) return new List<ReferralResponse>();

            var caseIds = referrals.Select(r => r.CaseId).Distinct().ToList();
            var cases = (await _caseRepository.SearchAsyncInclude(
                    c => caseIds.Contains(c.CaseId), c => c.Student, c => c.SupportCategory))
                .ToDictionary(c => c.CaseId);

            return referrals.Select(r =>
            {
                var caseRecord = cases.GetValueOrDefault(r.CaseId);
                return new ReferralResponse
                {
                    ReferralId = r.ReferralId,
                    CaseId = r.CaseId,
                    StudentId = caseRecord?.StudentId ?? Guid.Empty,
                    StudentName = caseRecord?.Student?.FullName ?? string.Empty,
                    CategoryName = caseRecord?.SupportCategory?.CategoryName ?? string.Empty,
                    SupportType = caseRecord?.SupportCategory?.SupportType ?? string.Empty,
                    CaseCurrentStage = caseRecord?.CurrentStage ?? string.Empty,
                    CasePriority = caseRecord?.Priority ?? string.Empty,
                    FromAccountId = r.FromAccountId,
                    FromAccountName = r.FromAccount?.FullName ?? string.Empty,
                    ToAccountId = r.ToAccountId,
                    ToAccountName = r.ToAccount?.FullName ?? string.Empty,
                    ReferralReason = r.ReferralReason,
                    Priority = r.Priority,
                    Status = r.Status,
                    CreatedAt = CaseRules.AsUtc(r.CreatedAt),
                    AcceptedAt = CaseRules.AsUtc(r.AcceptedAt)
                };
            }).ToList();
        }
    }
}
