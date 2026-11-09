using AdviseCare.Application.Contracts.Requests.Statistic;
using AdviseCare.Application.Contracts.Responses.Statistic;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IStatisticService
    {
        Task<List<GetWelfareStatisticResponse>> GetWelfareStatisticAsync(GetWelfareStatisticRequest request);
        Task<ExportWelfareStatisticResponse> ExportWelfareStatisticAsync(Guid AccessorId, ExportWelfareStatisticRequest request);
        Task<List<GetCohortStatisticResponse>> GetCohortStatisticAsync(Guid AccessorId, GetCohortStatisticRequest request);
        Task<ExportCohortStatisticResponse> ExportCohortStatisticAsync(Guid AccessorId, ExportCohortStatisticRequest request);
    }
}
