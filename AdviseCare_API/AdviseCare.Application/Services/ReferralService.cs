using AdviseCare.Application.Contracts.Requests.Referral;
using AdviseCare.Application.Contracts.Responses.Referral;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Referrals: the case's advisor refers it to another advisor (EX4) or to the Welfare Officer (WF2 step 8.2 / WF2b).
    // - an advisor who accepts becomes the case's advisor (stage referred -> assigned)
    // - the Welfare Officer who accepts does not take the case: the advisor keeps it and the stage stays referred
    // - a returned referral puts the case back to the stage it had before, with the reason in the stage history
    public class ReferralService : IReferralService
    {
        private static readonly string[] ReceiverRoles = { "Academic Advisor", "Wellbeing Advisor", "Student Welfare Officer" };

        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Casestagehistory> _caseStageHistoryRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public ReferralService(
            IGenericRepository<Referral> referralRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Casestagehistory> caseStageHistoryRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _referralRepository = referralRepository;
            _caseRepository = caseRepository;
            _caseStageHistoryRepository = caseStageHistoryRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Referrals, newest first (Programme Head)
        public async Task<IEnumerable<GetAllReferralResponse>> GetAll()
        {
            var referrals = await GetReferralsAsync(r => true);

            return referrals.Select(r => new GetAllReferralResponse
            {
                ReferralId = r.ReferralId,
                CaseId = r.CaseId,
                FromAccountId = r.FromAccountId,
                StudentId = r.Case.StudentId,
                ToAccountId = r.ToAccountId,
                CaseCurrentStage = r.Case.CurrentStage,
                CasePriority = r.Case.Priority,
                CategoryName = r.Case.SupportCategory.CategoryName,
                FromAccountFullName = r.FromAccount?.FullName ?? string.Empty,
                ReferralReason = r.ReferralReason,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                ToAccountFullName = r.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = r.AcceptedAt,
                CreatedAt = r.CreatedAt,
                Priority = r.Priority,
                Status = r.Status
            });
        }

        // Get all Referrals with pagination
        public async Task<IEnumerable<GetAllReferralWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var referrals = await GetReferralsAsync(r => true);

            return referrals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllReferralWithPaginationResponse
                {
                    ReferralId = r.ReferralId,
                    CaseId = r.CaseId,
                    FromAccountId = r.FromAccountId,
                    StudentId = r.Case.StudentId,
                    ToAccountId = r.ToAccountId,
                    CaseCurrentStage = r.Case.CurrentStage,
                    CasePriority = r.Case.Priority,
                    CategoryName = r.Case.SupportCategory.CategoryName,
                    FromAccountFullName = r.FromAccount?.FullName ?? string.Empty,
                    ReferralReason = r.ReferralReason,
                    StudentFullName = r.Case.Student.FullName,
                    SupportType = r.Case.SupportCategory.SupportType,
                    ToAccountFullName = r.ToAccount?.FullName ?? string.Empty,
                    AcceptedAt = r.AcceptedAt,
                    CreatedAt = r.CreatedAt,
                    Priority = r.Priority,
                    Status = r.Status
                });
        }

        // Get Referral by Id (its sender, its receiver, or the staff of its case)
        public async Task<GetReferralByIdResponse> GetByIdAsync(Guid CallerId, Guid ReferralId)
        {
            var referral = (await GetReferralsAsync(r => r.ReferralId == ReferralId)).FirstOrDefault()
                ?? throw new NotFoundException("Referral not found.");
            if (referral.FromAccountId != CallerId && referral.ToAccountId != CallerId && !await CanViewCaseAsStaffAsync(referral.Case, CallerId))
                throw new NotFoundException("Referral not found.");

            return new GetReferralByIdResponse
            {
                ReferralId = referral.ReferralId,
                CaseId = referral.CaseId,
                FromAccountId = referral.FromAccountId,
                StudentId = referral.Case.StudentId,
                ToAccountId = referral.ToAccountId,
                CaseCurrentStage = referral.Case.CurrentStage,
                CasePriority = referral.Case.Priority,
                CategoryName = referral.Case.SupportCategory.CategoryName,
                FromAccountFullName = referral.FromAccount?.FullName ?? string.Empty,
                ReferralReason = referral.ReferralReason,
                StudentFullName = referral.Case.Student.FullName,
                SupportType = referral.Case.SupportCategory.SupportType,
                ToAccountFullName = referral.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = referral.AcceptedAt,
                CreatedAt = referral.CreatedAt,
                Priority = referral.Priority,
                Status = referral.Status
            };
        }

        // Search Referrals by case, priority and status: with a case, the staff of that case; without one, the Programme Head only
        public async Task<List<SearchReferralResponse>> SearchAsync(Guid CallerId, SearchReferralRequest request)
        {
            if (request.CaseId != null)
            {
                var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId.Value)
                    ?? throw new NotFoundException("Case not found.");
                if (!await CanViewCaseAsStaffAsync(caseRecord, CallerId))
                    throw new NotFoundException("Case not found.");
            }
            else if (!await HasRoleAsync(CallerId, "Programme Head"))
                throw new ValidationException("A case is required. Use the incoming or outgoing list for your own referrals.");

            var referrals = await GetReferralsAsync(r =>
                (request.CaseId == null || r.CaseId == request.CaseId) &&
                (string.IsNullOrWhiteSpace(request.Priority) || r.Priority == request.Priority) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            return referrals.Select(r => new SearchReferralResponse
            {
                ReferralId = r.ReferralId,
                CaseId = r.CaseId,
                FromAccountId = r.FromAccountId,
                StudentId = r.Case.StudentId,
                ToAccountId = r.ToAccountId,
                CaseCurrentStage = r.Case.CurrentStage,
                CasePriority = r.Case.Priority,
                CategoryName = r.Case.SupportCategory.CategoryName,
                FromAccountFullName = r.FromAccount?.FullName ?? string.Empty,
                ReferralReason = r.ReferralReason,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                ToAccountFullName = r.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = r.AcceptedAt,
                CreatedAt = r.CreatedAt,
                Priority = r.Priority,
                Status = r.Status
            }).ToList();
        }

        // Create Referral: the case's advisor refers it to an active Academic Advisor, Wellbeing Advisor or Student Welfare Officer;
        // the case moves to referred (stage history row) and the receiver is notified
        public async Task<CreateReferralResponse> CreateAsync(Guid CreatorId, CreateReferralRequest request)
        {
            var caseRecord = await GetCaseAsync(request.CaseId);
            if (caseRecord.AdvisorId != CreatorId)
            {
                if (!await CanViewCaseAsStaffAsync(caseRecord, CreatorId))
                    throw new NotFoundException("Case not found.");
                throw new ForbiddenException("Only the case's current advisor can refer it.");
            }
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (await _referralRepository.ExistsAsync(r => r.CaseId == caseRecord.CaseId && r.Status == "pending"))
                throw new ConflictException("This case already has a referral waiting for an answer.");
            if (request.ToAccountId == CreatorId)
                throw new ValidationException("You cannot refer a case to yourself.");

            var receiver = await _accountRepository.GetByIdAsync(request.ToAccountId)
                ?? throw new NotFoundException("Receiver account not found.");
            if (receiver.Status != "active")
                throw new ValidationException($"{receiver.FullName} is not an active account.");
            if (!(await GetRoleNamesAsync(receiver.AccountId)).Overlaps(ReceiverRoles))
                throw new ValidationException($"{receiver.FullName} is not an Academic Advisor, Wellbeing Advisor or Student Welfare Officer.");

            var referralReason = request.ReferralReason.Trim();
            if (referralReason.Length == 0)
                throw new ValidationException("Referral reason is required.");

            var now = DateTime.UtcNow;
            var referral = new Referral
            {
                ReferralId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                FromAccountId = CreatorId,
                ToAccountId = receiver.AccountId,
                ReferralReason = referralReason,
                CreatedAt = now,
                Priority = request.Priority,
                Status = "pending"
            };
            _referralRepository.PrepareCreate(referral);
            AddStageHistory(caseRecord, CreatorId, "referred", $"Referred to {receiver.FullName}: {referralReason}", now);
            _caseRepository.PrepareUpdate(caseRecord);
            await _referralRepository.SaveAsync();

            await _activityLogService.LogAsync(CreatorId, "referralSent",
                $"Referred the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to {receiver.FullName} ({referral.Priority})");
            await _notificationService.NotifyAsync(receiver.AccountId, "New referral",
                $"{caseRecord.Advisor?.FullName} referred the {caseRecord.SupportCategory.CategoryName} case of {caseRecord.Student.FullName} to you ({referral.Priority} priority).",
                $"/referrals/{referral.ReferralId}");

            var created = (await GetReferralsAsync(r => r.ReferralId == referral.ReferralId)).First();

            return new CreateReferralResponse
            {
                ReferralId = created.ReferralId,
                CaseId = created.CaseId,
                FromAccountId = created.FromAccountId,
                StudentId = created.Case.StudentId,
                ToAccountId = created.ToAccountId,
                CaseCurrentStage = created.Case.CurrentStage,
                CasePriority = created.Case.Priority,
                CategoryName = created.Case.SupportCategory.CategoryName,
                FromAccountFullName = created.FromAccount?.FullName ?? string.Empty,
                ReferralReason = created.ReferralReason,
                StudentFullName = created.Case.Student.FullName,
                SupportType = created.Case.SupportCategory.SupportType,
                ToAccountFullName = created.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = created.AcceptedAt,
                CreatedAt = created.CreatedAt,
                Priority = created.Priority,
                Status = created.Status
            };
        }

        // Get the Referrals sent to the signed-in account, newest first, optionally by status
        public async Task<List<GetMyIncomingReferralResponse>> GetMyIncomingAsync(Guid AccountId, SearchMyReferralRequest request)
        {
            var referrals = await GetReferralsAsync(r => r.ToAccountId == AccountId && (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            return referrals.Select(r => new GetMyIncomingReferralResponse
            {
                ReferralId = r.ReferralId,
                CaseId = r.CaseId,
                FromAccountId = r.FromAccountId,
                StudentId = r.Case.StudentId,
                ToAccountId = r.ToAccountId,
                CaseCurrentStage = r.Case.CurrentStage,
                CasePriority = r.Case.Priority,
                CategoryName = r.Case.SupportCategory.CategoryName,
                FromAccountFullName = r.FromAccount?.FullName ?? string.Empty,
                ReferralReason = r.ReferralReason,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                ToAccountFullName = r.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = r.AcceptedAt,
                CreatedAt = r.CreatedAt,
                Priority = r.Priority,
                Status = r.Status
            }).ToList();
        }

        // Get the Referrals sent by the signed-in advisor, newest first, optionally by status
        public async Task<List<GetMyOutgoingReferralResponse>> GetMyOutgoingAsync(Guid AccountId, SearchMyReferralRequest request)
        {
            var referrals = await GetReferralsAsync(r => r.FromAccountId == AccountId && (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            return referrals.Select(r => new GetMyOutgoingReferralResponse
            {
                ReferralId = r.ReferralId,
                CaseId = r.CaseId,
                FromAccountId = r.FromAccountId,
                StudentId = r.Case.StudentId,
                ToAccountId = r.ToAccountId,
                CaseCurrentStage = r.Case.CurrentStage,
                CasePriority = r.Case.Priority,
                CategoryName = r.Case.SupportCategory.CategoryName,
                FromAccountFullName = r.FromAccount?.FullName ?? string.Empty,
                ReferralReason = r.ReferralReason,
                StudentFullName = r.Case.Student.FullName,
                SupportType = r.Case.SupportCategory.SupportType,
                ToAccountFullName = r.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = r.AcceptedAt,
                CreatedAt = r.CreatedAt,
                Priority = r.Priority,
                Status = r.Status
            }).ToList();
        }

        // Accept Referral: the receiver accepts; an advisor becomes the case's advisor (referred -> assigned, the student gets the new contact),
        // the Welfare Officer works on the case while the advisor keeps it; the sender is notified
        public async Task<AcceptReferralResponse> AcceptAsync(Guid ReceiverId, Guid ReferralId)
        {
            var referral = await GetPendingForReceiverAsync(ReceiverId, ReferralId);
            var caseRecord = await GetCaseAsync(referral.CaseId);
            var receiver = await _accountRepository.GetByIdAsync(ReceiverId)
                ?? throw new NotFoundException("Account not found.");
            var receiverRoles = await GetRoleNamesAsync(ReceiverId);
            var isWelfareReferral = receiverRoles.Contains("Student Welfare Officer") && !receiverRoles.Contains("Academic Advisor") && !receiverRoles.Contains("Wellbeing Advisor");
            var sender = await _accountRepository.GetByIdAsync(referral.FromAccountId);

            var now = DateTime.UtcNow;
            referral.Status = "accepted";
            referral.AcceptedAt = now;
            _referralRepository.PrepareUpdate(referral);
            if (!isWelfareReferral)
            {
                caseRecord.AdvisorId = ReceiverId;
                caseRecord.Advisor = receiver;
                AddStageHistory(caseRecord, ReceiverId, "assigned", $"Referral accepted by {receiver.FullName}: {referral.ReferralReason}", now);
                _caseRepository.PrepareUpdate(caseRecord);
            }
            await _referralRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(ReceiverId, "referralAnswered", $"Accepted the referral of the {categoryName} case of {caseRecord.Student.FullName} from {sender?.FullName}");
            await _notificationService.NotifyAsync(referral.FromAccountId, "Referral accepted", isWelfareReferral
                    ? $"{receiver.FullName} accepted your referral of the {categoryName} case of {caseRecord.Student.FullName}. The case comes back to you for follow-up once the welfare work is done."
                    : $"{receiver.FullName} accepted your referral of the {categoryName} case of {caseRecord.Student.FullName} and is now handling it.",
                $"/referrals/{referral.ReferralId}");
            if (!isWelfareReferral)
            {
                await _notificationService.NotifyAsync(caseRecord.StudentId, "Your case has a new advisor",
                    $"{receiver.FullName} ({receiver.Email}) is now handling your {categoryName} case. Please contact them to arrange a meeting.",
                    $"/cases/{caseRecord.CaseId}");
            }

            var accepted = (await GetReferralsAsync(r => r.ReferralId == ReferralId)).First();

            return new AcceptReferralResponse
            {
                ReferralId = accepted.ReferralId,
                CaseId = accepted.CaseId,
                FromAccountId = accepted.FromAccountId,
                StudentId = accepted.Case.StudentId,
                ToAccountId = accepted.ToAccountId,
                CaseCurrentStage = accepted.Case.CurrentStage,
                CasePriority = accepted.Case.Priority,
                CategoryName = accepted.Case.SupportCategory.CategoryName,
                FromAccountFullName = accepted.FromAccount?.FullName ?? string.Empty,
                ReferralReason = accepted.ReferralReason,
                StudentFullName = accepted.Case.Student.FullName,
                SupportType = accepted.Case.SupportCategory.SupportType,
                ToAccountFullName = accepted.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = accepted.AcceptedAt,
                CreatedAt = accepted.CreatedAt,
                Priority = accepted.Priority,
                Status = accepted.Status
            };
        }

        // Return Referral: the receiver returns it with a reason; the case goes back to the stage it had before the referral
        // (the reason is written to the stage history) and the sender is notified
        public async Task<ReturnReferralResponse> ReturnAsync(Guid ReceiverId, Guid ReferralId, ReturnReferralRequest request)
        {
            var referral = await GetPendingForReceiverAsync(ReceiverId, ReferralId);
            var caseRecord = await GetCaseAsync(referral.CaseId);
            var receiver = await _accountRepository.GetByIdAsync(ReceiverId)
                ?? throw new NotFoundException("Account not found.");
            var reason = request.Reason.Trim();
            if (reason.Length == 0)
                throw new ValidationException("Reason is required.");

            var referredRow = (await _caseStageHistoryRepository.SearchAsyncOrderBy(h => h.CaseId == caseRecord.CaseId && h.ToStage == "referred", h => h.ChangedAt, false)).FirstOrDefault();
            var previousStage = referredRow == null || referredRow.FromStage == "referred" ? "assigned" : referredRow.FromStage;

            referral.Status = "returned";
            _referralRepository.PrepareUpdate(referral);
            if (caseRecord.CurrentStage == "referred")
            {
                AddStageHistory(caseRecord, ReceiverId, previousStage, $"Referral returned by {receiver.FullName}: {reason}", DateTime.UtcNow);
                _caseRepository.PrepareUpdate(caseRecord);
            }
            await _referralRepository.SaveAsync();

            var categoryName = caseRecord.SupportCategory.CategoryName;
            await _activityLogService.LogAsync(ReceiverId, "referralAnswered", $"Returned the referral of the {categoryName} case of {caseRecord.Student.FullName}: {reason}");
            await _notificationService.NotifyAsync(referral.FromAccountId, "Referral returned",
                $"{receiver.FullName} returned your referral of the {categoryName} case of {caseRecord.Student.FullName}: {reason}",
                $"/cases/{caseRecord.CaseId}");

            var returned = (await GetReferralsAsync(r => r.ReferralId == ReferralId)).First();

            return new ReturnReferralResponse
            {
                ReferralId = returned.ReferralId,
                CaseId = returned.CaseId,
                FromAccountId = returned.FromAccountId,
                StudentId = returned.Case.StudentId,
                ToAccountId = returned.ToAccountId,
                CaseCurrentStage = returned.Case.CurrentStage,
                CasePriority = returned.Case.Priority,
                CategoryName = returned.Case.SupportCategory.CategoryName,
                FromAccountFullName = returned.FromAccount?.FullName ?? string.Empty,
                ReferralReason = returned.ReferralReason,
                StudentFullName = returned.Case.Student.FullName,
                SupportType = returned.Case.SupportCategory.SupportType,
                ToAccountFullName = returned.ToAccount?.FullName ?? string.Empty,
                AcceptedAt = returned.AcceptedAt,
                CreatedAt = returned.CreatedAt,
                Priority = returned.Priority,
                Status = returned.Status
            };
        }

        // Helper: load Referrals with their case (student, category), sender and receiver, newest first
        private async Task<List<Referral>> GetReferralsAsync(Expression<Func<Referral, bool>> Predicate)
        {
            return await _referralRepository.SearchAsyncIncludeOrderBy(Predicate, r => r.CreatedAt, false,
                r => r.Case.Student, r => r.Case.SupportCategory, r => r.FromAccount, r => r.ToAccount);
        }

        // Helper: load one Case with its student, advisor and category
        private async Task<Case> GetCaseAsync(Guid CaseId)
        {
            return await _caseRepository.GetByIdAsyncInclude(CaseId, c => c.Student, c => c.Advisor, c => c.SupportCategory)
                ?? throw new NotFoundException("Case not found.");
        }

        // Helper: a pending referral that the caller received
        private async Task<Referral> GetPendingForReceiverAsync(Guid ReceiverId, Guid ReferralId)
        {
            var referral = await _referralRepository.GetByIdAsync(ReferralId)
                ?? throw new NotFoundException("Referral not found.");
            if (referral.ToAccountId != ReceiverId)
            {
                if (referral.FromAccountId == ReceiverId)
                    throw new ForbiddenException("Only the receiver can answer a referral.");
                throw new NotFoundException("Referral not found.");
            }
            if (referral.Status != "pending")
                throw new ConflictException($"The referral was already {referral.Status}.");
            return referral;
        }

        // Helper: the staff of a case are its current advisor, the Programme Head, and anyone who sent or received a referral of it (never the student)
        private async Task<bool> CanViewCaseAsStaffAsync(Case CaseRecord, Guid CallerId)
        {
            if (CaseRecord.AdvisorId == CallerId)
                return true;
            if (CaseRecord.StudentId == CallerId)
                return false;
            if (await HasRoleAsync(CallerId, "Programme Head"))
                return true;
            return await _referralRepository.ExistsAsync(r => r.CaseId == CaseRecord.CaseId && (r.FromAccountId == CallerId || r.ToAccountId == CallerId));
        }

        // Helper: stage one CASESTAGEHISTORIES row (insert-only) and move the case to the new stage; the caller saves both together
        private void AddStageHistory(Case CaseRecord, Guid ChangedBy, string ToStage, string Reason, DateTime ChangedAt)
        {
            _caseStageHistoryRepository.PrepareCreate(new Casestagehistory
            {
                CaseStageHistoryId = Guid.NewGuid(),
                CaseId = CaseRecord.CaseId,
                ChangedBy = ChangedBy,
                Reason = Reason,
                ChangedAt = ChangedAt,
                FromStage = CaseRecord.CurrentStage,
                ToStage = ToStage
            });
            CaseRecord.CurrentStage = ToStage;
        }

        // Helper: the role names of an account
        private async Task<HashSet<string>> GetRoleNamesAsync(Guid AccountId)
        {
            return (await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == AccountId, ar => ar.Role))
                .Select(ar => ar.Role.RoleName)
                .ToHashSet();
        }

        // Helper: whether an account has a role
        private async Task<bool> HasRoleAsync(Guid AccountId, string RoleName)
        {
            return await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == AccountId && ar.Role.RoleName == RoleName);
        }
    }
}
