using AdviseCare.Application.Contracts.Requests.ExternalService;
using AdviseCare.Application.Contracts.Responses.ExternalService;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class ExternalServiceService(IGenericRepository<Externalservice> externalServiceRepository) : IExternalServiceService
    {
        private readonly IGenericRepository<Externalservice> _externalServiceRepository = externalServiceRepository;

        public async Task<IEnumerable<GetAllExternalServiceResponse>> GetAll()
        {
            var services = await _externalServiceRepository.GetAllAsyncOrderBy(service => service.ServiceName);
            return services.Select(ToGetAllResponse);
        }

        public async Task<IEnumerable<GetAllExternalServiceWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var services = await _externalServiceRepository.GetAllAsyncOrderBy(service => service.ServiceName);
            return services
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(service => new GetAllExternalServiceWithPaginationResponse
                {
                    ExternalServiceId = service.ExternalServiceId,
                    ContactInfo = service.ContactInfo ?? string.Empty,
                    ServiceName = service.ServiceName,
                    ServiceType = service.ServiceType ?? string.Empty,
                    IsActive = service.IsActive
                });
        }

        public async Task<GetExternalServiceByIdResponse> GetByIdAsync(Guid externalServiceId)
        {
            var service = await _externalServiceRepository.GetByIdAsync(externalServiceId)
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

        public async Task<List<SearchExternalServiceResponse>> SearchAsync(SearchExternalServiceRequest request)
        {
            var services = await _externalServiceRepository.SearchAsyncOrderBy(service =>
                (string.IsNullOrWhiteSpace(request.ServiceName) || service.ServiceName.Contains(request.ServiceName.Trim())) &&
                (string.IsNullOrWhiteSpace(request.ServiceType) || (service.ServiceType != null && service.ServiceType.Contains(request.ServiceType.Trim()))) &&
                (!request.IsActive.HasValue || service.IsActive == request.IsActive.Value),
                service => service.ServiceName);

            return services.Select(service => new SearchExternalServiceResponse
            {
                ExternalServiceId = service.ExternalServiceId,
                ContactInfo = service.ContactInfo ?? string.Empty,
                ServiceName = service.ServiceName,
                ServiceType = service.ServiceType ?? string.Empty,
                IsActive = service.IsActive
            }).ToList();
        }

        private static GetAllExternalServiceResponse ToGetAllResponse(Externalservice service) => new()
        {
            ExternalServiceId = service.ExternalServiceId,
            ContactInfo = service.ContactInfo ?? string.Empty,
            ServiceName = service.ServiceName,
            ServiceType = service.ServiceType ?? string.Empty,
            IsActive = service.IsActive
        };
    }
}
