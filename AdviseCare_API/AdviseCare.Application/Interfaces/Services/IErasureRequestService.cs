using AdviseCare.Application.Contracts.Requests.ErasureRequest;
using AdviseCare.Application.Contracts.Responses.ErasureRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IErasureRequestService
    {
        Task<IEnumerable<GetAllErasureRequestResponse>> GetAll();
        Task<IEnumerable<GetAllErasureRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetErasureRequestByIdResponse> GetByIdAsync(Guid ErasureRequestId);
        Task<List<SearchErasureRequestResponse>> SearchAsync(SearchErasureRequestRequest request);
        Task<bool> ExecuteAsync(Guid StewardId, Guid ErasureRequestId);
    }
}
