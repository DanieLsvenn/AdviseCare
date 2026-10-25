using AdviseCare.Application.Contracts.Requests.WelfareStatistics;
using AdviseCare.Application.Contracts.Responses.WelfareStatistics;

namespace AdviseCare.Application.Interfaces.Services;

public interface IWelfareStatisticsService
{
    Task<IReadOnlyList<WelfareStatisticsByCategoryResponse>> GetByCategoryAsync(
        SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken);

    Task<WelfareStatisticsExportFile> ExportCsvAsync(
        SearchWelfareStatisticsRequest request,
        CancellationToken cancellationToken);
}
