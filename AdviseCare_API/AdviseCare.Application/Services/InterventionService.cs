using AdviseCare.Application.Contracts.Requests.Intervention;
using AdviseCare.Application.Contracts.Responses.Intervention;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class InterventionService(
        IGenericRepository<Intervention> interventionRepository,
        IGenericRepository<Case> caseRepository,
        INotificationService notificationService) : IInterventionService
    {
        private readonly IGenericRepository<Intervention> _interventionRepository = interventionRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;
        private readonly INotificationService _notificationService = notificationService;

        public async Task<InterventionResponse> CreateAsync(Guid officerId, CreateInterventionRequest request)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(request.CaseId)
                ?? throw new NotFoundException("Case not found.");

            var intervention = new Intervention
            {
                InterventionId = Guid.NewGuid(),
                CaseId = request.CaseId,
                OfficerId = officerId,
                Notes = NormalizeOptionalValue(request.Notes),
                InterventionDate = GetUtcDate(request.InterventionDate),
                InterventionType = request.InterventionType.Trim(),
                IsClosed = false,
                Status = request.Status.Trim(),
                Case = caseRecord
            };

            await _interventionRepository.CreateAsync(intervention);
            return ToResponse(intervention);
        }

        public async Task<List<InterventionResponse>> SearchAsync(SearchInterventionRequest request)
        {
            if (!request.StudentId.HasValue && !request.CaseId.HasValue)
                throw new ValidationException("Specify a student id or case id to search interventions.");

            var interventions = await _interventionRepository.SearchAsyncIncludeOrderBy(
                intervention =>
                    (!request.StudentId.HasValue || intervention.Case.StudentId == request.StudentId.Value) &&
                    (!request.CaseId.HasValue || intervention.CaseId == request.CaseId.Value) &&
                    (string.IsNullOrWhiteSpace(request.Status) || intervention.Status == request.Status.Trim()),
                intervention => intervention.InterventionDate,
                false,
                intervention => intervention.Case);

            return interventions.Select(ToResponse).ToList();
        }

        public async Task<InterventionResponse> UpdateAsync(
            Guid officerId,
            Guid interventionId,
            UpdateInterventionRequest request)
        {
            var intervention = await GetOwnedOpenInterventionAsync(officerId, interventionId);

            intervention.Notes = NormalizeOptionalValue(request.Notes);
            intervention.InterventionDate = GetUtcDate(request.InterventionDate);
            intervention.InterventionType = request.InterventionType.Trim();
            intervention.Status = request.Status.Trim();
            await _interventionRepository.UpdateAsync(intervention);

            return ToResponse(intervention);
        }

        public async Task<InterventionResponse> RecordOutcomeAsync(
            Guid officerId,
            Guid interventionId,
            RecordInterventionOutcomeRequest request)
        {
            var intervention = await GetOwnedOpenInterventionAsync(officerId, interventionId);
            var outcome = request.Outcome.Trim();
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ValidationException("Outcome is required.");

            intervention.Outcome = outcome;
            await _interventionRepository.UpdateAsync(intervention);

            return ToResponse(intervention);
        }

        public async Task<InterventionResponse> CloseAsync(
            Guid officerId,
            Guid interventionId,
            CloseInterventionRequest request)
        {
            var intervention = await GetOwnedOpenInterventionAsync(officerId, interventionId);
            var outcome = request.Outcome.Trim();
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ValidationException("Outcome is required.");

            var advisorId = intervention.Case.AdvisorId;
            intervention.Outcome = outcome;
            intervention.IsClosed = true;
            intervention.Status = "completed";
            await _interventionRepository.UpdateAsync(intervention);

            if (advisorId.HasValue)
            {
                await _notificationService.NotifyAsync(
                    advisorId.Value,
                    "Welfare intervention completed",
                    $"The welfare intervention for case {intervention.CaseId} is complete.",
                    $"/cases/{intervention.CaseId}");
            }

            return ToResponse(intervention);
        }

        private async Task<Intervention> GetOwnedOpenInterventionAsync(Guid officerId, Guid interventionId)
        {
            var intervention = await _interventionRepository.GetByIdAsyncInclude(interventionId, item => item.Case)
                ?? throw new NotFoundException("Intervention not found.");

            if (intervention.OfficerId != officerId)
                throw new NotFoundException("Intervention not found.");
            if (intervention.IsClosed)
                throw new ConflictException("A closed intervention cannot be changed.");

            return intervention;
        }

        private static DateTime GetUtcDate(DateTimeOffset? date)
        {
            if (!date.HasValue)
                throw new ValidationException("Intervention date is required.");
            return date.Value.UtcDateTime;
        }

        private static string? NormalizeOptionalValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static InterventionResponse ToResponse(Intervention intervention) => new()
        {
            InterventionId = intervention.InterventionId,
            CaseId = intervention.CaseId,
            StudentId = intervention.Case.StudentId,
            OfficerId = intervention.OfficerId,
            Notes = intervention.Notes ?? string.Empty,
            Outcome = intervention.Outcome ?? string.Empty,
            InterventionDate = DateTime.SpecifyKind(intervention.InterventionDate, DateTimeKind.Utc),
            InterventionType = intervention.InterventionType,
            IsClosed = intervention.IsClosed,
            Status = intervention.Status
        };
    }
}
