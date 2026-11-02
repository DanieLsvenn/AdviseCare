using AdviseCare.Application.Contracts.Requests.ExternalService;
using AdviseCare.Application.Contracts.Responses.ExternalService;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class ExternalServiceService : IExternalServiceService
    {
        private readonly IGenericRepository<Externalservice> _externalServiceRepository;
        private readonly IActivityLogService _activityLogService;

        public ExternalServiceService(
            IGenericRepository<Externalservice> externalServiceRepository,
            IActivityLogService activityLogService)
        {
            _externalServiceRepository = externalServiceRepository;
            _activityLogService = activityLogService;
        }

        // Get all External services
        public async Task<IEnumerable<GetAllExternalServiceResponse>> GetAll()
        {
            var services = await _externalServiceRepository.GetAllAsyncOrderBy(s => s.ServiceName, true);

            return services.Select(s => new GetAllExternalServiceResponse
            {
                ExternalServiceId = s.ExternalServiceId,
                ContactInfo = s.ContactInfo ?? string.Empty,
                ServiceName = s.ServiceName,
                ServiceType = s.ServiceType ?? string.Empty,
                IsActive = s.IsActive
            });
        }

        // Get all External services with pagination
        public async Task<IEnumerable<GetAllExternalServiceWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var services = await _externalServiceRepository.GetAllAsyncOrderBy(s => s.ServiceName, true);

            return services
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new GetAllExternalServiceWithPaginationResponse
                {
                    ExternalServiceId = s.ExternalServiceId,
                    ContactInfo = s.ContactInfo ?? string.Empty,
                    ServiceName = s.ServiceName,
                    ServiceType = s.ServiceType ?? string.Empty,
                    IsActive = s.IsActive
                });
        }

        // Get External service by Id
        public async Task<GetExternalServiceByIdResponse> GetByIdAsync(Guid ExternalServiceId)
        {
            var service = await _externalServiceRepository.GetByIdAsync(ExternalServiceId)
                ?? throw new NotFoundException("External service not found.");

            return new GetExternalServiceByIdResponse
            {
                ExternalServiceId = service.ExternalServiceId,
                ContactInfo = service.ContactInfo ?? string.Empty,
                ServiceName = service.ServiceName,
                ServiceType = service.ServiceType ?? string.Empty,
                IsActive = service.IsActive
            };
        }

        // Search External services by name or type (contains) and active state
        public async Task<List<SearchExternalServiceResponse>> SearchAsync(SearchExternalServiceRequest request)
        {
            var services = await _externalServiceRepository.SearchAsyncOrderBy(s =>
                (string.IsNullOrWhiteSpace(request.ServiceName) || s.ServiceName.Contains(request.ServiceName)) &&
                (string.IsNullOrWhiteSpace(request.ServiceType) || (s.ServiceType != null && s.ServiceType.Contains(request.ServiceType))) &&
                (request.IsActive == null || s.IsActive == request.IsActive),
                s => s.ServiceName, true);

            return services.Select(s => new SearchExternalServiceResponse
            {
                ExternalServiceId = s.ExternalServiceId,
                ContactInfo = s.ContactInfo ?? string.Empty,
                ServiceName = s.ServiceName,
                ServiceType = s.ServiceType ?? string.Empty,
                IsActive = s.IsActive
            }).ToList();
        }

        // Create External service (active)
        public async Task<CreateExternalServiceResponse> CreateAsync(CreateExternalServiceRequest request)
        {
            var service = new Externalservice
            {
                ExternalServiceId = Guid.NewGuid(),
                ContactInfo = string.IsNullOrWhiteSpace(request.ContactInfo) ? null : request.ContactInfo.Trim(),
                ServiceName = request.ServiceName.Trim(),
                ServiceType = string.IsNullOrWhiteSpace(request.ServiceType) ? null : request.ServiceType.Trim(),
                IsActive = true
            };
            await _externalServiceRepository.CreateAsync(service);

            await _activityLogService.LogCurrentAccountAsync("externalServiceCreated", $"Added external service {service.ServiceName}");

            return new CreateExternalServiceResponse
            {
                ExternalServiceId = service.ExternalServiceId,
                ContactInfo = service.ContactInfo ?? string.Empty,
                ServiceName = service.ServiceName,
                ServiceType = service.ServiceType ?? string.Empty,
                IsActive = service.IsActive
            };
        }

        // Update External service (an inactive service stays in old referrals but cannot receive new ones)
        public async Task<UpdateExternalServiceResponse> UpdateAsync(Guid ExternalServiceId, UpdateExternalServiceRequest request)
        {
            var service = await _externalServiceRepository.GetByIdAsync(ExternalServiceId)
                ?? throw new NotFoundException("External service not found.");

            service.ContactInfo = string.IsNullOrWhiteSpace(request.ContactInfo) ? null : request.ContactInfo.Trim();
            service.ServiceName = request.ServiceName.Trim();
            service.ServiceType = string.IsNullOrWhiteSpace(request.ServiceType) ? null : request.ServiceType.Trim();
            service.IsActive = request.IsActive;
            await _externalServiceRepository.UpdateAsync(service);

            await _activityLogService.LogCurrentAccountAsync("externalServiceUpdated", $"Updated external service {service.ServiceName} ({(service.IsActive ? "active" : "inactive")})");

            return new UpdateExternalServiceResponse
            {
                ExternalServiceId = service.ExternalServiceId,
                ContactInfo = service.ContactInfo ?? string.Empty,
                ServiceName = service.ServiceName,
                ServiceType = service.ServiceType ?? string.Empty,
                IsActive = service.IsActive
            };
        }
    }
}
