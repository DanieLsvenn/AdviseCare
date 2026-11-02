using AdviseCare.Application.Contracts.Responses.Case;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Shared logic of the Member D services (cases, notes, referrals, case change requests).
    // CASES.advisorId is the only advisor-student link: an advisor "advises" a student while they hold a case of that student that is not closed.
    // Methods named Stage* only stage changes on the shared DbContext; the calling service commits them with one SaveAsync,
    // so a case update and its stage history row are saved together or not at all.
    public class CaseWorkflowHelper(
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Casestagehistory> stageHistoryRepository,
        IGenericRepository<Referral> referralRepository,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<AccountRole> accountRoleRepository,
        IGenericRepository<Studentprofile> studentProfileRepository)
    {
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Casestagehistory> _stageHistoryRepository = stageHistoryRepository;
        private readonly IGenericRepository<Referral> _referralRepository = referralRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository = accountRoleRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository = studentProfileRepository;

        // ---------- Accounts and roles ----------

        public async Task<HashSet<string>> GetRoleNamesAsync(Guid accountId)
        {
            var accountRoles = await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == accountId, ar => ar.Role);
            return accountRoles.Select(ar => ar.Role.RoleName).ToHashSet();
        }

        public async Task<List<Account>> GetActiveAccountsInRoleAsync(string roleName)
        {
            var accountRoles = await _accountRoleRepository.SearchAsyncInclude(
                ar => ar.Role.RoleName == roleName && ar.Account.Status == CaseRules.Active,
                ar => ar.Account);
            return accountRoles.Select(ar => ar.Account).OrderBy(a => a.FullName).ToList();
        }

        // Loads an account that is active and has one of the given roles; returns the account and all its roles
        public async Task<(Account Advisor, HashSet<string> Roles)> GetActiveAdvisorAsync(Guid advisorId, params string[] requiredAnyRole)
        {
            var advisor = await _accountRepository.GetByIdAsync(advisorId)
                ?? throw new NotFoundException("Advisor account not found.");
            if (advisor.Status != CaseRules.Active)
                throw new ValidationException($"{advisor.FullName} is not an active account.");

            var roles = await GetRoleNamesAsync(advisorId);
            if (!requiredAnyRole.Any(roles.Contains))
                throw new ValidationException($"{advisor.FullName} does not have the role {string.Join(" or ", requiredAnyRole)}.");

            return (advisor, roles);
        }

        // ---------- Cases ----------

        public async Task<Case> GetCaseWithDetailsAsync(Guid caseId)
        {
            return await _caseRepository.GetByIdAsyncInclude(
                caseId,
                c => c.Student,
                c => c.Advisor,
                c => c.OpenedByNavigation,
                c => c.Subject,
                c => c.SupportCategory)
                ?? throw new NotFoundException("Case not found.");
        }

        // Who may see a case: the student it belongs to, its current advisor, the Programme Head,
        // and anyone who sent or received a referral of it (e.g. the Welfare Officer).
        public async Task<bool> CanViewCaseAsync(Case caseRecord, Guid accountId, HashSet<string> roles)
        {
            if (caseRecord.StudentId == accountId) return true;
            if (caseRecord.AdvisorId == accountId) return true;
            if (roles.Contains(CaseRules.ProgrammeHeadRole)) return true;
            return await _referralRepository.ExistsAsync(r =>
                r.CaseId == caseRecord.CaseId && (r.FromAccountId == accountId || r.ToAccountId == accountId));
        }

        // Staff view only: the student may see their own case and its stage history, but not the notes
        public async Task<bool> CanViewCaseAsStaffAsync(Case caseRecord, Guid accountId, HashSet<string> roles)
        {
            if (caseRecord.StudentId == accountId && caseRecord.AdvisorId != accountId) return false;
            return await CanViewCaseAsync(caseRecord, accountId, roles);
        }

        public async Task EnsureCanViewCaseAsync(Case caseRecord, Guid accountId, HashSet<string> roles)
        {
            // Not found instead of forbidden, so a case id cannot be probed
            if (!await CanViewCaseAsync(caseRecord, accountId, roles))
                throw new NotFoundException("Case not found.");
        }

        public Task<bool> HasPendingReferralAsync(Guid caseId) =>
            _referralRepository.ExistsAsync(r => r.CaseId == caseId && r.Status == CaseRules.Pending);

        // Adds one CASESTAGEHISTORIES row (insert-only) and moves the case to the new stage
        public void StageStageChange(Case caseRecord, Guid changedBy, string toStage, string reason, DateTime now)
        {
            _stageHistoryRepository.PrepareCreate(new Casestagehistory
            {
                CaseStageHistoryId = Guid.NewGuid(),
                CaseId = caseRecord.CaseId,
                ChangedBy = changedBy,
                Reason = reason,
                ChangedAt = now,
                FromStage = caseRecord.CurrentStage,
                ToStage = toStage
            });
            caseRecord.CurrentStage = toStage;
        }

        // Moves a case to another advisor without changing its stage (direct reassign, approved case change request).
        // The stage history row has the same stage on both sides: it records the hand-over, not a stage change.
        public void StageAdvisorChange(Case caseRecord, Account newAdvisor, Guid changedBy, string reason, DateTime now)
        {
            var oldName = caseRecord.Advisor?.FullName ?? "no advisor";
            caseRecord.AdvisorId = newAdvisor.AccountId;
            caseRecord.Advisor = newAdvisor;
            StageStageChange(caseRecord, changedBy, caseRecord.CurrentStage,
                $"Advisor changed from {oldName} to {newAdvisor.FullName}: {reason}", now);
            _caseRepository.PrepareUpdate(caseRecord);
        }

        // ---------- Mapping ----------

        // Cases must be loaded with Student, Advisor, OpenedByNavigation, Subject and SupportCategory included
        public async Task<List<CaseResponse>> ToCaseResponsesAsync(List<Case> cases)
        {
            if (cases.Count == 0) return new List<CaseResponse>();

            var studentIds = cases.Select(c => c.StudentId).Distinct().ToList();
            var studentCodes = (await _studentProfileRepository.SearchAsync(p => studentIds.Contains(p.StudentId)))
                .ToDictionary(p => p.StudentId, p => p.StudentCode);

            return cases.Select(c => new CaseResponse
            {
                CaseId = c.CaseId,
                StudentId = c.StudentId,
                StudentName = c.Student?.FullName ?? string.Empty,
                StudentCode = studentCodes.GetValueOrDefault(c.StudentId) ?? string.Empty,
                StudentEmail = c.Student?.Email ?? string.Empty,
                AdvisorId = c.AdvisorId,
                AdvisorName = c.Advisor?.FullName ?? string.Empty,
                AdvisorEmail = c.Advisor?.Email ?? string.Empty,
                OpenedBy = c.OpenedBy,
                OpenedByName = c.OpenedByNavigation?.FullName ?? string.Empty,
                SubjectId = c.SubjectId,
                SubjectCode = c.Subject?.SubjectCode ?? string.Empty,
                SubjectName = c.Subject?.SubjectName ?? string.Empty,
                SupportCategoryId = c.SupportCategoryId,
                CategoryName = c.SupportCategory?.CategoryName ?? string.Empty,
                SupportType = c.SupportCategory?.SupportType ?? string.Empty,
                ConsentPurposeId = c.SupportCategory?.ConsentPurposeId ?? Guid.Empty,
                RequestReason = c.RequestReason,
                OpenedAt = CaseRules.AsUtc(c.OpenedAt),
                ClosedAt = CaseRules.AsUtc(c.ClosedAt),
                CurrentStage = c.CurrentStage,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();
        }

        public async Task<CaseResponse> ToCaseResponseAsync(Case caseRecord) =>
            (await ToCaseResponsesAsync(new List<Case> { caseRecord }))[0];

        // ---------- Input checks ----------

        public static string RequireText(string? value, string message)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                throw new ValidationException(message);
            return trimmed;
        }

        public static string? OptionalFilter(string? value, string[] allowed, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            if (!allowed.Contains(trimmed))
                throw new ValidationException($"{name} must be one of: {string.Join(", ", allowed)}.");
            return trimmed;
        }
    }
}
