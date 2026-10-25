using AdviseCare.Application.Contracts.Requests.CohortIndicator;
using AdviseCare.Application.Contracts.Responses.CohortIndicator;

namespace AdviseCare.Application.Interfaces.Services;

public interface ICohortIndicatorService
{
    Task<IReadOnlyList<CohortIndicatorSummaryResponse>> GetSummaryAsync(
        Guid accessorId,
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken);

    Task<CohortIndicatorExportFile> ExportCsvAsync(
        Guid accessorId,
        SearchCohortIndicatorSummaryRequest request,
        CancellationToken cancellationToken);
}
