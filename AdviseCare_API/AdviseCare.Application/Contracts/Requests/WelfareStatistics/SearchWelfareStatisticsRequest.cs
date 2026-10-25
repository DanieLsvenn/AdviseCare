namespace AdviseCare.Application.Contracts.Requests.WelfareStatistics;

public sealed class SearchWelfareStatisticsRequest
{
    public Guid? TermId { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }
}
