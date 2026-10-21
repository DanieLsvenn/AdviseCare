using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Contracts.Responses.Indicator;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IIndicatorService
    {
        Task<IEnumerable<GetAllIndicatorResponse>> GetAllAsync();
        Task<IEnumerable<GetAllIndicatorWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetIndicatorByIdResponse> GetByIdAsync(Guid id);
        Task<List<SearchIndicatorResponse>> SearchAsync(SearchIndicatorRequest request);
        Task<CreateIndicatorResponse> CreateAsync(CreateIndicatorRequest request);
        Task<UpdateIndicatorResponse> UpdateAsync(Guid id, UpdateIndicatorRequest request);
        Task<ArchiveIndicatorResponse> ArchiveAsync(Guid id);
    }
}
