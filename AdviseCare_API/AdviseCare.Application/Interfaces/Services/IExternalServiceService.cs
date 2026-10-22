using AdviseCare.Application.Contracts.Requests.ExternalService;
using AdviseCare.Application.Contracts.Responses.ExternalService;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IExternalServiceService
    {
        Task<IEnumerable<GetAllExternalServiceResponse>> GetAll();
        Task<IEnumerable<GetAllExternalServiceWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetExternalServiceByIdResponse> GetByIdAsync(Guid externalServiceId);
        Task<List<SearchExternalServiceResponse>> SearchAsync(SearchExternalServiceRequest request);
    }
}
