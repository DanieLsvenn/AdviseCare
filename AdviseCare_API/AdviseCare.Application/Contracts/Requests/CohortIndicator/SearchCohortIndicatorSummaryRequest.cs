namespace AdviseCare.Application.Contracts.Requests.CohortIndicator;

public sealed class SearchCohortIndicatorSummaryRequest
{
    public Guid? TermId { get; init; }

    public Guid? FacultyId { get; init; }
}
