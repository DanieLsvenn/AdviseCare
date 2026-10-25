namespace AdviseCare.Application.Contracts.Responses.StudentIndicator;

public sealed class StudentIndicatorProfileResponse
{
    public Guid StudentId { get; set; }

    public Guid? CaseId { get; set; }

    public DateOnly? AsOfDate { get; set; }

    public List<StudentIndicatorProfileValueResponse> Indicators { get; set; } = new();
}

public sealed class StudentIndicatorProfileValueResponse
{
    public Guid IndicatorId { get; set; }

    public string IndicatorName { get; set; } = string.Empty;

    public Guid TermId { get; set; }

    public string TermName { get; set; } = string.Empty;

    public Guid? DataSourceId { get; set; }

    public DateTime? TransactionFrom { get; set; }

    public DateTime? ValidFrom { get; set; }

    public string? ProvenanceNote { get; set; }

    public bool IsShared { get; set; }

    public bool HasValue { get; set; }

    public bool IsMissing { get; set; }

    public bool IsErased { get; set; }

    public string? Value { get; set; }
}
