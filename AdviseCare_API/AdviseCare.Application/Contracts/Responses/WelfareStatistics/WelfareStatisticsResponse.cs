namespace AdviseCare.Application.Contracts.Responses.WelfareStatistics;

public sealed record WelfareStatisticsByCategoryResponse(
    string CategoryName,
    int CaseCount,
    int StudentCount);

public sealed record WelfareStatisticsExportFile(
    byte[] Content,
    string ContentType,
    string FileName);
