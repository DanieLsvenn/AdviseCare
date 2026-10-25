using AdviseCare.Application.Contracts.Requests.SupportGrant;
using AdviseCare.Application.Contracts.Responses.SupportGrant;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class SupportGrantService(
        IGenericRepository<Supportgrant> supportGrantRepository,
        IGenericRepository<Intervention> interventionRepository) : ISupportGrantService
    {
        private readonly IGenericRepository<Supportgrant> _supportGrantRepository = supportGrantRepository;
        private readonly IGenericRepository<Intervention> _interventionRepository = interventionRepository;

        public async Task<SupportGrantResponse> CreateAsync(Guid officerId, CreateSupportGrantRequest request)
        {
            var intervention = await _interventionRepository.GetByIdAsync(request.InterventionId)
                ?? throw new NotFoundException("Intervention not found.");

            if (intervention.OfficerId != officerId)
                throw new NotFoundException("Intervention not found.");
            if (intervention.IsClosed)
                throw new ConflictException("A support grant cannot be added to a closed intervention.");

            var grantType = request.GrantType.Trim();
            if (string.IsNullOrWhiteSpace(grantType))
                throw new ValidationException("Grant type is required.");
            if (!request.IssuedAt.HasValue)
                throw new ValidationException("Issue date is required.");

            var grant = new Supportgrant
            {
                SupportGrantId = Guid.NewGuid(),
                InterventionId = request.InterventionId,
                GrantDescription = NormalizeOptionalValue(request.GrantDescription),
                GrantType = grantType,
                IssuedAt = request.IssuedAt.Value.UtcDateTime,
                Status = "issued"
            };

            await _supportGrantRepository.CreateAsync(grant);
            return ToResponse(grant);
        }

        public async Task<List<SupportGrantResponse>> SearchAsync(Guid officerId, SearchSupportGrantRequest request)
        {
            if (request.InterventionId == Guid.Empty)
                throw new ValidationException("Intervention id is required.");

            var intervention = await _interventionRepository.GetByIdAsync(request.InterventionId)
                ?? throw new NotFoundException("Intervention not found.");

            if (intervention.OfficerId != officerId)
                throw new NotFoundException("Intervention not found.");

            var grants = await _supportGrantRepository.SearchAsyncOrderBy(
                grant => grant.InterventionId == request.InterventionId,
                grant => grant.IssuedAt,
                false);

            return grants.Select(ToResponse).ToList();
        }

        private static string? NormalizeOptionalValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static SupportGrantResponse ToResponse(Supportgrant grant) => new()
        {
            SupportGrantId = grant.SupportGrantId,
            InterventionId = grant.InterventionId,
            GrantDescription = grant.GrantDescription ?? string.Empty,
            GrantType = grant.GrantType,
            IssuedAt = DateTime.SpecifyKind(grant.IssuedAt, DateTimeKind.Utc),
            Status = grant.Status
        };
    }
}
