using AdviseCare.Application.Contracts.Requests.Indicator;
using AdviseCare.Application.Contracts.Responses.Indicator;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IIndicatorService
    {
        Task<IEnumerable<GetAllIndicatorResponse>> GetAll();
        Task<IEnumerable<GetAllIndicatorWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetIndicatorByIdResponse> GetByIdAsync(Guid IndicatorId);
        Task<List<SearchIndicatorResponse>> SearchAsync(SearchIndicatorRequest request);
        Task<CreateIndicatorResponse> CreateAsync(CreateIndicatorRequest request);
        Task<UpdateIndicatorResponse> UpdateAsync(Guid IndicatorId, UpdateIndicatorRequest request);
        Task<bool> ArchiveAsync(Guid IndicatorId);
    }
}
