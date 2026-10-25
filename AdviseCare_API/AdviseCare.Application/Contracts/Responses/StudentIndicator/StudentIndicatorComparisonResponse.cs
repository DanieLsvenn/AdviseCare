namespace AdviseCare.Application.Contracts.Responses.StudentIndicator;

public sealed class StudentIndicatorComparisonResponse
{
    public Guid StudentId { get; set; }

    public Guid CaseId { get; set; }

    public DateOnly DateA { get; set; }

    public DateOnly DateB { get; set; }

    public List<StudentIndicatorComparisonValueResponse> Indicators { get; set; } = new();
}

public sealed class StudentIndicatorComparisonValueResponse
{
    public Guid IndicatorId { get; set; }

    public string IndicatorName { get; set; } = string.Empty;

    public Guid TermId { get; set; }

    public string TermName { get; set; } = string.Empty;

    public StudentIndicatorProfileValueResponse? DateAValue { get; set; }

    public StudentIndicatorProfileValueResponse? DateBValue { get; set; }
}
