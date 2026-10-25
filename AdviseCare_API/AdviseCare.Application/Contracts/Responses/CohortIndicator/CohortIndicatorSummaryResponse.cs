namespace AdviseCare.Application.Contracts.Responses.CohortIndicator;

public sealed record CohortIndicatorSummaryResponse(
    Guid IndicatorId,
    string IndicatorName,
    string DataType,
    Guid TermId,
    string TermName,
    string? CategoryValue,
    decimal? MeanValue,
    int StudentCount);

public sealed record CohortIndicatorExportFile(
    byte[] Content,
    string ContentType,
    string FileName);
