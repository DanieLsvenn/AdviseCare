using AdviseCare.Application.Contracts.Requests.IndicatorFlag;
using AdviseCare.Application.Contracts.Responses.IndicatorFlag;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IIndicatorFlagService
    {
        Task<IEnumerable<GetAllIndicatorFlagResponse>> GetAll(Guid CallerId);
        Task<IEnumerable<GetAllIndicatorFlagWithPaginationResponse>> GetAllWithPaginationAsync(Guid CallerId, int page, int pageSize);
        Task<GetIndicatorFlagByIdResponse> GetByIdAsync(Guid CallerId, Guid IndicatorFlagId);
        Task<List<SearchIndicatorFlagResponse>> SearchAsync(Guid CallerId, SearchIndicatorFlagRequest request);
        Task<CheckIndicatorFlagResponse> CheckAsync(Guid CallerId);
        Task<DismissIndicatorFlagResponse> DismissAsync(Guid ProgrammeHeadId, Guid IndicatorFlagId, DismissIndicatorFlagRequest request);
        Task<OpenCaseFromIndicatorFlagResponse> OpenCaseAsync(Guid ProgrammeHeadId, Guid IndicatorFlagId, OpenCaseFromIndicatorFlagRequest request);
    }
}
