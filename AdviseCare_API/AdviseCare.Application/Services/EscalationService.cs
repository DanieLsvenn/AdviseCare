using AdviseCare.Application.Contracts.Requests.Escalation;
using AdviseCare.Application.Contracts.Responses.Escalation;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class EscalationService(
        IGenericRepository<Escalation> escalationRepository,
        IGenericRepository<Case> caseRepository,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<AccountRole> accountRoleRepository,
        INotificationService notificationService) : IEscalationService
    {
        private const string ProgrammeHeadRole = "Programme Head";
        private readonly IGenericRepository<Escalation> _escalationRepository = escalationRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository = accountRoleRepository;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<EscalationResponse> CreateAsync(Guid escalatedBy, CreateEscalationRequest request)
        {
            if (!await _caseRepository.ExistsAsync(caseRecord => caseRecord.CaseId == request.CaseId))
                throw new NotFoundException("Case not found.");

            var programmeHead = await _accountRepository.GetByIdAsync(request.EscalatedTo)
                ?? throw new NotFoundException("Programme Head account not found.");
            if (programmeHead.Status != "active")
                throw new ValidationException("Escalations can only be sent to an active Programme Head.");

            if (!await _accountRoleRepository.ExistsAsync(accountRole =>
                accountRole.AccountId == request.EscalatedTo && accountRole.Role.RoleName == ProgrammeHeadRole))
                throw new ValidationException("The selected account does not have the Programme Head role.");

            var reason = request.Reason.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                throw new ValidationException("Escalation reason is required.");

            var escalation = new Escalation
            {
                EscalationId = Guid.NewGuid(),
                CaseId = request.CaseId,
                EscalatedBy = escalatedBy,
                EscalatedTo = request.EscalatedTo,
                Reason = reason,
                EscalatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            await _escalationRepository.CreateAsync(escalation);
            await _notificationService.NotifyAsync(
                escalation.EscalatedTo,
                "Welfare escalation",
                $"A welfare escalation was raised for case {escalation.CaseId}.",
                $"/escalations/{escalation.EscalationId}");

            return await GetByIdAsync(escalatedBy, escalation.EscalationId);
        }

        public async Task<List<EscalationResponse>> SearchAsync(
            Guid accountId,
            bool isProgrammeHead,
            SearchEscalationRequest request)
        {
            var escalations = await _escalationRepository.SearchAsyncIncludeOrderBy(
                escalation =>
                    (isProgrammeHead ? escalation.EscalatedTo == accountId : escalation.EscalatedBy == accountId) &&
                    (!request.CaseId.HasValue || escalation.CaseId == request.CaseId.Value) &&
                    (string.IsNullOrWhiteSpace(request.Status) || escalation.Status == request.Status.Trim()),
                escalation => escalation.EscalatedAt,
                false,
                escalation => escalation.EscalatedByNavigation,
                escalation => escalation.EscalatedToNavigation);

            return escalations.Select(ToResponse).ToList();
        }

        public async Task<EscalationResponse> GetByIdAsync(Guid accountId, Guid escalationId)
        {
            var escalation = await _escalationRepository.GetByIdAsyncInclude(
                escalationId,
                item => item.EscalatedByNavigation,
                item => item.EscalatedToNavigation)
                ?? throw new NotFoundException("Escalation not found.");

            if (escalation.EscalatedBy != accountId && escalation.EscalatedTo != accountId)
                throw new NotFoundException("Escalation not found.");

            return ToResponse(escalation);
        }

        public async Task<EscalationResponse> RespondAsync(
            Guid programmeHeadId,
            Guid escalationId,
            RespondToEscalationRequest request)
        {
            var escalation = await _escalationRepository.GetByIdAsyncInclude(
                escalationId,
                item => item.EscalatedByNavigation,
                item => item.EscalatedToNavigation)
                ?? throw new NotFoundException("Escalation not found.");

            if (escalation.EscalatedTo != programmeHeadId)
                throw new NotFoundException("Escalation not found.");
            if (escalation.Status != "pending")
                throw new ConflictException("Only pending escalations can be answered.");

            var response = request.Response.Trim();
            if (string.IsNullOrWhiteSpace(response))
                throw new ValidationException("Response is required.");

            escalation.Response = response;
            escalation.Status = "responded";
            await _escalationRepository.UpdateAsync(escalation);
            await _notificationService.NotifyAsync(
                escalation.EscalatedBy,
                "Welfare escalation response",
                $"The Programme Head responded to the escalation for case {escalation.CaseId}.",
                $"/escalations/{escalation.EscalationId}");

            return ToResponse(escalation);
        }

        private static EscalationResponse ToResponse(Escalation escalation) => new()
        {
            EscalationId = escalation.EscalationId,
            CaseId = escalation.CaseId,
            EscalatedBy = escalation.EscalatedBy,
            EscalatedByName = escalation.EscalatedByNavigation?.FullName ?? string.Empty,
            EscalatedTo = escalation.EscalatedTo,
            EscalatedToName = escalation.EscalatedToNavigation?.FullName ?? string.Empty,
            Reason = escalation.Reason,
            Response = escalation.Response ?? string.Empty,
            EscalatedAt = DateTime.SpecifyKind(escalation.EscalatedAt, DateTimeKind.Utc),
            Status = escalation.Status
        };
    }
}
