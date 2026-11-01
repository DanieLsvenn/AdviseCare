using AdviseCare.Application.Contracts.Responses.AdvisorAssignment;
using AdviseCare.Application.Contracts.Responses.Case;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    // Shared logic of the Member D services (cases, notes, referrals, assignments, change requests).
    // Methods named Stage* only stage changes on the shared DbContext; the calling service commits them with one SaveAsync,
    // so a case update, its stage history row and its assignment row are saved together or not at all.
    public class CaseWorkflowHelper(
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Casestagehistory> stageHistoryRepository,
        IGenericRepository<Advisorassignment> assignmentRepository,
        IGenericRepository<Referral> referralRepository,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<AccountRole> accountRoleRepository,
        IGenericRepository<Studentprofile> studentProfileRepository)
    {
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Casestagehistory> _stageHistoryRepository = stageHistoryRepository;
        private readonly IGenericRepository<Advisorassignment> _assignmentRepository = assignmentRepository;
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

        // Loads an advisor that is active and has one of the given roles; returns the account and all its roles
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

        // ---------- Advisor assignments ----------

        // Returns the active assignment of this advisor and student, or stages a new one ("create or keep")
        public async Task<Advisorassignment> StageEnsureAssignmentAsync(Guid advisorId, Guid studentId, Guid assignedBy, DateTime now)
        {
            var existing = await _assignmentRepository.FirstOrDefaultAsync(a =>
                a.AdvisorId == advisorId && a.StudentId == studentId && a.Status == CaseRules.Active);
            if (existing != null)
                return existing;

            var assignment = new Advisorassignment
            {
                AdvisorAssignmentId = Guid.NewGuid(),
                AdvisorId = advisorId,
                StudentId = studentId,
                AssignedBy = assignedBy,
                AssignedAt = now,
                Status = CaseRules.Active
            };
            _assignmentRepository.PrepareCreate(assignment);
            return assignment;
        }

        // EX6: revoke the old assignment, create/keep the new advisor's assignment with the same student,
        // and move the open cases of the old pair to the new advisor when the new advisor can handle their support type.
        // Only stages changes; the caller saves.
        public async Task<(Advisorassignment NewAssignment, List<Guid> Moved, List<Guid> Kept)> StageHandoverAsync(
            Advisorassignment oldAssignment,
            Account oldAdvisor,
            Account newAdvisor,
            HashSet<string> newAdvisorRoles,
            Guid actorId,
            string reason,
            DateTime now)
        {
            oldAssignment.Status = CaseRules.Revoked;
            oldAssignment.RevokedAt = now;
            _assignmentRepository.PrepareUpdate(oldAssignment);

            var newAssignment = await StageEnsureAssignmentAsync(newAdvisor.AccountId, oldAssignment.StudentId, actorId, now);

            var openCases = await _caseRepository.SearchAsyncInclude(
                c => c.AdvisorId == oldAssignment.AdvisorId &&
                     c.StudentId == oldAssignment.StudentId &&
                     c.Status != CaseRules.StatusClosed,
                c => c.SupportCategory);
            var openCaseIds = openCases.Select(c => c.CaseId).ToList();
            var pendingReferralCaseIds = (await _referralRepository.SearchAsync(r =>
                    r.Status == CaseRules.Pending && openCaseIds.Contains(r.CaseId)))
                .Select(r => r.CaseId)
                .ToHashSet();

            var moved = new List<Guid>();
            var kept = new List<Guid>();
            foreach (var caseRecord in openCases)
            {
                var canHandle = newAdvisorRoles.Contains(CaseRules.AdvisorRoleFor(caseRecord.SupportCategory.SupportType));
                if (!canHandle || pendingReferralCaseIds.Contains(caseRecord.CaseId))
                {
                    kept.Add(caseRecord.CaseId);
                    continue;
                }

                caseRecord.AdvisorId = newAdvisor.AccountId;
                // Same stage on both sides: the row records the hand-over, not a stage change
                StageStageChange(caseRecord, actorId, caseRecord.CurrentStage,
                    $"Advisor changed from {oldAdvisor.FullName} to {newAdvisor.FullName}: {reason}", now);
                _caseRepository.PrepareUpdate(caseRecord);
                moved.Add(caseRecord.CaseId);
            }

            return (newAssignment, moved, kept);
        }

        public async Task<List<AdvisorAssignmentResponse>> ToAssignmentResponsesAsync(List<Advisorassignment> assignments)
        {
            if (assignments.Count == 0) return new List<AdvisorAssignmentResponse>();

            var accountIds = assignments.SelectMany(a => new[] { a.AdvisorId, a.StudentId, a.AssignedBy }).Distinct().ToList();
            var accounts = (await _accountRepository.SearchAsync(a => accountIds.Contains(a.AccountId))).ToDictionary(a => a.AccountId);
            var studentIds = assignments.Select(a => a.StudentId).Distinct().ToList();
            var studentCodes = (await _studentProfileRepository.SearchAsync(p => studentIds.Contains(p.StudentId)))
                .ToDictionary(p => p.StudentId, p => p.StudentCode);
            var advisorIds = assignments.Select(a => a.AdvisorId).Distinct().ToList();
            var openCases = await _caseRepository.SearchAsync(c =>
                c.AdvisorId != null && advisorIds.Contains(c.AdvisorId.Value) &&
                studentIds.Contains(c.StudentId) &&
                c.Status != CaseRules.StatusClosed);

            return assignments.Select(a => new AdvisorAssignmentResponse
            {
                AdvisorAssignmentId = a.AdvisorAssignmentId,
                AdvisorId = a.AdvisorId,
                AdvisorName = accounts.GetValueOrDefault(a.AdvisorId)?.FullName ?? string.Empty,
                AdvisorEmail = accounts.GetValueOrDefault(a.AdvisorId)?.Email ?? string.Empty,
                StudentId = a.StudentId,
                StudentName = accounts.GetValueOrDefault(a.StudentId)?.FullName ?? string.Empty,
                StudentCode = studentCodes.GetValueOrDefault(a.StudentId) ?? string.Empty,
                StudentEmail = accounts.GetValueOrDefault(a.StudentId)?.Email ?? string.Empty,
                AssignedBy = a.AssignedBy,
                AssignedByName = accounts.GetValueOrDefault(a.AssignedBy)?.FullName ?? string.Empty,
                AssignedAt = CaseRules.AsUtc(a.AssignedAt),
                RevokedAt = CaseRules.AsUtc(a.RevokedAt),
                Status = a.Status,
                OpenCaseCount = openCases.Count(c => c.AdvisorId == a.AdvisorId && c.StudentId == a.StudentId)
            }).ToList();
        }

        // ---------- Mapping ----------

        // Cases must be loaded with Student, Advisor, OpenedByNavigation, Subject and SupportCategory included
        public async Task<List<CaseResponse>> ToCaseResponsesAsync(List<Case> cases)
        {
            if (cases.Count == 0) return new List<CaseResponse>();

            var studentIds = cases.Select(c => c.StudentId).Distinct().ToList();
            var studentCodes = (await _studentProfileRepository.SearchAsync(p => studentIds.Contains(p.StudentId)))
                .ToDictionary(p => p.StudentId, p => p.StudentCode);
            var advisorIds = cases.Where(c => c.AdvisorId != null).Select(c => c.AdvisorId!.Value).Distinct().ToList();
            var assignments = advisorIds.Count == 0
                ? new List<Advisorassignment>()
                : await _assignmentRepository.SearchAsync(a =>
                    a.Status == CaseRules.Active && advisorIds.Contains(a.AdvisorId) && studentIds.Contains(a.StudentId));

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
                AdvisorAssignmentId = assignments
                    .FirstOrDefault(a => a.AdvisorId == c.AdvisorId && a.StudentId == c.StudentId)?.AdvisorAssignmentId,
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
