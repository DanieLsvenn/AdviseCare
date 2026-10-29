using AdviseCare.Application.Contracts.Requests.SupportGrant;
using AdviseCare.Application.Contracts.Responses.SupportGrant;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // Support grants: support issued under an intervention (issued -> revoked)
    public class SupportGrantService : ISupportGrantService
    {
        private readonly IGenericRepository<Supportgrant> _supportGrantRepository;
        private readonly IGenericRepository<Intervention> _interventionRepository;
        private readonly IActivityLogService _activityLogService;

        public SupportGrantService(
            IGenericRepository<Supportgrant> supportGrantRepository,
            IGenericRepository<Intervention> interventionRepository,
            IActivityLogService activityLogService)
        {
            _supportGrantRepository = supportGrantRepository;
            _interventionRepository = interventionRepository;
            _activityLogService = activityLogService;
        }

        // Get all Support grants
        public async Task<IEnumerable<GetAllSupportGrantResponse>> GetAll()
        {
            var grants = await GetGrantsAsync(g => true);

            return grants.Select(g => new GetAllSupportGrantResponse
            {
                SupportGrantId = g.SupportGrantId,
                InterventionId = g.InterventionId,
                GrantDescription = g.GrantDescription ?? string.Empty,
                GrantType = g.GrantType,
                IssuedAt = g.IssuedAt,
                Status = g.Status
            });
        }

        // Get all Support grants with pagination
        public async Task<IEnumerable<GetAllSupportGrantWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var grants = await GetGrantsAsync(g => true);

            return grants
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GetAllSupportGrantWithPaginationResponse
                {
                    SupportGrantId = g.SupportGrantId,
                    InterventionId = g.InterventionId,
                    GrantDescription = g.GrantDescription ?? string.Empty,
                    GrantType = g.GrantType,
                    IssuedAt = g.IssuedAt,
                    Status = g.Status
                });
        }

        // Get Support grant by Id
        public async Task<GetSupportGrantByIdResponse> GetByIdAsync(Guid SupportGrantId)
        {
            var grant = await _supportGrantRepository.GetByIdAsync(SupportGrantId)
                ?? throw new NotFoundException("Support grant not found.");

            return new GetSupportGrantByIdResponse
            {
                SupportGrantId = grant.SupportGrantId,
                InterventionId = grant.InterventionId,
                GrantDescription = grant.GrantDescription ?? string.Empty,
                GrantType = grant.GrantType,
                IssuedAt = grant.IssuedAt,
                Status = grant.Status
            };
        }

        // Search Support grants by intervention or status
        public async Task<List<SearchSupportGrantResponse>> SearchAsync(SearchSupportGrantRequest request)
        {
            var grants = await GetGrantsAsync(g =>
                (request.InterventionId == null || g.InterventionId == request.InterventionId) &&
                (string.IsNullOrWhiteSpace(request.Status) || g.Status == request.Status));

            return grants.Select(g => new SearchSupportGrantResponse
            {
                SupportGrantId = g.SupportGrantId,
                InterventionId = g.InterventionId,
                GrantDescription = g.GrantDescription ?? string.Empty,
                GrantType = g.GrantType,
                IssuedAt = g.IssuedAt,
                Status = g.Status
            }).ToList();
        }

        // Create Support grant under the officer's own open intervention
        public async Task<CreateSupportGrantResponse> CreateAsync(Guid CreatorId, CreateSupportGrantRequest request)
        {
            var intervention = await _interventionRepository.GetByIdAsync(request.InterventionId)
                ?? throw new NotFoundException("Intervention not found.");
            if (intervention.OfficerId != CreatorId)
                throw new ForbiddenException("Only the Welfare Officer of the intervention can record a grant under it.");
            if (intervention.IsClosed)
                throw new ConflictException("A support grant cannot be added to a closed intervention.");

            var grant = new Supportgrant
            {
                SupportGrantId = Guid.NewGuid(),
                InterventionId = request.InterventionId,
                GrantDescription = string.IsNullOrWhiteSpace(request.GrantDescription) ? null : request.GrantDescription.Trim(),
                GrantType = request.GrantType.Trim(),
                IssuedAt = request.IssuedAt.ToUniversalTime(),
                Status = "issued"
            };
            await _supportGrantRepository.CreateAsync(grant);

            await _activityLogService.LogAsync(CreatorId, "supportGrantRecorded", $"Recorded a {grant.GrantType} grant under the intervention of case {intervention.CaseId}");

            return new CreateSupportGrantResponse
            {
                SupportGrantId = grant.SupportGrantId,
                InterventionId = grant.InterventionId,
                GrantDescription = grant.GrantDescription ?? string.Empty,
                GrantType = grant.GrantType,
                IssuedAt = grant.IssuedAt,
                Status = grant.Status
            };
        }

        // Revoke Support grant (issued -> revoked) by the officer of its intervention
        public async Task<bool> RevokeAsync(Guid OfficerId, Guid SupportGrantId)
        {
            var grant = (await GetGrantsAsync(g => g.SupportGrantId == SupportGrantId)).FirstOrDefault()
                ?? throw new NotFoundException("Support grant not found.");
            if (grant.Intervention.OfficerId != OfficerId)
                throw new ForbiddenException("Only the Welfare Officer of the intervention can revoke its grants.");
            if (grant.Status == "revoked")
                throw new ConflictException("This support grant is already revoked.");

            grant.Status = "revoked";
            await _supportGrantRepository.SaveAsync();

            await _activityLogService.LogAsync(OfficerId, "supportGrantRevoked", $"Revoked the {grant.GrantType} grant under the intervention of case {grant.Intervention.CaseId}");
            return true;
        }

        // Helper: load Support grants with their intervention, newest first
        private async Task<List<Supportgrant>> GetGrantsAsync(Expression<Func<Supportgrant, bool>> predicate)
        {
            return await _supportGrantRepository.SearchAsyncIncludeOrderBy(predicate, g => g.IssuedAt, false, g => g.Intervention);
        }
    }
}
