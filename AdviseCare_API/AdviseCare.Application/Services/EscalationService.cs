using AdviseCare.Application.Contracts.Requests.Escalation;
using AdviseCare.Application.Contracts.Responses.Escalation;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Escalations: the Welfare Officer raises a case beyond their authority to a Programme Head, who responds (pending -> responded).
    // Each side only sees the escalations they raised or that were addressed to them
    public class EscalationService : IEscalationService
    {
        private readonly IGenericRepository<Escalation> _escalationRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public EscalationService(
            IGenericRepository<Escalation> escalationRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Referral> referralRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _escalationRepository = escalationRepository;
            _caseRepository = caseRepository;
            _referralRepository = referralRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Escalations the signed-in account raised or received
        public async Task<IEnumerable<GetAllEscalationResponse>> GetAll(Guid AccountId)
        {
            var escalations = await GetEscalationsAsync(e => e.EscalatedBy == AccountId || e.EscalatedTo == AccountId);

            return escalations.Select(e => new GetAllEscalationResponse
            {
                EscalationId = e.EscalationId,
                CaseId = e.CaseId,
                EscalatedBy = e.EscalatedBy,
                EscalatedTo = e.EscalatedTo,
                EscalatedByFullName = e.EscalatedByNavigation.FullName,
                EscalatedToFullName = e.EscalatedToNavigation.FullName,
                Reason = e.Reason,
                Response = e.Response ?? string.Empty,
                EscalatedAt = e.EscalatedAt,
                Status = e.Status
            });
        }

        // Get all Escalations the signed-in account raised or received, with pagination
        public async Task<IEnumerable<GetAllEscalationWithPaginationResponse>> GetAllWithPaginationAsync(Guid AccountId, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var escalations = await GetEscalationsAsync(e => e.EscalatedBy == AccountId || e.EscalatedTo == AccountId);

            return escalations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new GetAllEscalationWithPaginationResponse
                {
                    EscalationId = e.EscalationId,
                    CaseId = e.CaseId,
                    EscalatedBy = e.EscalatedBy,
                    EscalatedTo = e.EscalatedTo,
                    EscalatedByFullName = e.EscalatedByNavigation.FullName,
                    EscalatedToFullName = e.EscalatedToNavigation.FullName,
                    Reason = e.Reason,
                    Response = e.Response ?? string.Empty,
                    EscalatedAt = e.EscalatedAt,
                    Status = e.Status
                });
        }

        // Get Escalation by Id (only for the account that raised or received it)
        public async Task<GetEscalationByIdResponse> GetByIdAsync(Guid AccountId, Guid EscalationId)
        {
            var escalation = (await GetEscalationsAsync(e => e.EscalationId == EscalationId && (e.EscalatedBy == AccountId || e.EscalatedTo == AccountId))).FirstOrDefault()
                ?? throw new NotFoundException("Escalation not found.");

            return new GetEscalationByIdResponse
            {
                EscalationId = escalation.EscalationId,
                CaseId = escalation.CaseId,
                EscalatedBy = escalation.EscalatedBy,
                EscalatedTo = escalation.EscalatedTo,
                EscalatedByFullName = escalation.EscalatedByNavigation.FullName,
                EscalatedToFullName = escalation.EscalatedToNavigation.FullName,
                Reason = escalation.Reason,
                Response = escalation.Response ?? string.Empty,
                EscalatedAt = escalation.EscalatedAt,
                Status = escalation.Status
            };
        }

        // Search the signed-in account's Escalations by case or status
        public async Task<List<SearchEscalationResponse>> SearchAsync(Guid AccountId, SearchEscalationRequest request)
        {
            var escalations = await GetEscalationsAsync(e =>
                (e.EscalatedBy == AccountId || e.EscalatedTo == AccountId) &&
                (request.CaseId == null || e.CaseId == request.CaseId) &&
                (string.IsNullOrWhiteSpace(request.Status) || e.Status == request.Status));

            return escalations.Select(e => new SearchEscalationResponse
            {
                EscalationId = e.EscalationId,
                CaseId = e.CaseId,
                EscalatedBy = e.EscalatedBy,
                EscalatedTo = e.EscalatedTo,
                EscalatedByFullName = e.EscalatedByNavigation.FullName,
                EscalatedToFullName = e.EscalatedToNavigation.FullName,
                Reason = e.Reason,
                Response = e.Response ?? string.Empty,
                EscalatedAt = e.EscalatedAt,
                Status = e.Status
            }).ToList();
        }

        // Create Escalation to an active Programme Head for an open case referred to (and accepted by) this Welfare Officer; the Programme Head is notified
        public async Task<CreateEscalationResponse> CreateAsync(Guid CreatorId, CreateEscalationRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (!await _referralRepository.ExistsAsync(r => r.CaseId == request.CaseId && r.ToAccountId == CreatorId && r.Status == "accepted"))
                throw new ForbiddenException("This case has not been referred to you (or you have not accepted the referral yet).");

            var programmeHead = await _accountRepository.GetByIdAsync(request.EscalatedTo)
                ?? throw new NotFoundException("Programme Head not found.");
            if (programmeHead.Status != "active" || !await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == programmeHead.AccountId && ar.Role.RoleName == "Programme Head"))
                throw new ValidationException("An escalation can only be sent to an active Programme Head.");

            var escalation = new Escalation
            {
                EscalationId = Guid.NewGuid(),
                CaseId = request.CaseId,
                EscalatedBy = CreatorId,
                EscalatedTo = request.EscalatedTo,
                Reason = request.Reason.Trim(),
                EscalatedAt = DateTime.UtcNow,
                Status = "pending"
            };
            await _escalationRepository.CreateAsync(escalation);

            await _notificationService.NotifyAsync(escalation.EscalatedTo, "Welfare escalation",
                "A Welfare Officer escalated a case to you.", $"/escalations/{escalation.EscalationId}");
            await _activityLogService.LogAsync(CreatorId, "escalationRaised", $"Escalated case {escalation.CaseId} to {programmeHead.FullName}");

            var created = (await GetEscalationsAsync(e => e.EscalationId == escalation.EscalationId)).Single();
            return new CreateEscalationResponse
            {
                EscalationId = created.EscalationId,
                CaseId = created.CaseId,
                EscalatedBy = created.EscalatedBy,
                EscalatedTo = created.EscalatedTo,
                EscalatedByFullName = created.EscalatedByNavigation.FullName,
                EscalatedToFullName = created.EscalatedToNavigation.FullName,
                Reason = created.Reason,
                Response = created.Response ?? string.Empty,
                EscalatedAt = created.EscalatedAt,
                Status = created.Status
            };
        }

        // Respond to an Escalation (only the Programme Head it was sent to, only while pending); the Welfare Officer is notified
        public async Task<RespondEscalationResponse> RespondAsync(Guid ProgrammeHeadId, Guid EscalationId, RespondEscalationRequest request)
        {
            var escalation = (await GetEscalationsAsync(e => e.EscalationId == EscalationId && e.EscalatedTo == ProgrammeHeadId)).FirstOrDefault()
                ?? throw new NotFoundException("Escalation not found.");
            if (escalation.Status != "pending")
                throw new ConflictException("Only a pending escalation can be answered.");

            escalation.Response = request.Response.Trim();
            escalation.Status = "responded";
            await _escalationRepository.SaveAsync();

            await _notificationService.NotifyAsync(escalation.EscalatedBy, "Escalation answered",
                "The Programme Head responded to your escalation.", $"/escalations/{escalation.EscalationId}");
            await _activityLogService.LogAsync(ProgrammeHeadId, "escalationResponded", $"Responded to the escalation of case {escalation.CaseId}");

            return new RespondEscalationResponse
            {
                EscalationId = escalation.EscalationId,
                CaseId = escalation.CaseId,
                EscalatedBy = escalation.EscalatedBy,
                EscalatedTo = escalation.EscalatedTo,
                EscalatedByFullName = escalation.EscalatedByNavigation.FullName,
                EscalatedToFullName = escalation.EscalatedToNavigation.FullName,
                Reason = escalation.Reason,
                Response = escalation.Response,
                EscalatedAt = escalation.EscalatedAt,
                Status = escalation.Status
            };
        }

        // Helper: load Escalations with both accounts, newest first
        private async Task<List<Escalation>> GetEscalationsAsync(Expression<Func<Escalation, bool>> predicate)
        {
            return await _escalationRepository.SearchAsyncIncludeOrderBy(predicate, e => e.EscalatedAt, false,
                e => e.EscalatedByNavigation, e => e.EscalatedToNavigation);
        }
    }
}
