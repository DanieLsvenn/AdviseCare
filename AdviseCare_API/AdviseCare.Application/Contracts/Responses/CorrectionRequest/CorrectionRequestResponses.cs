namespace AdviseCare.Application.Contracts.Responses.CorrectionRequest;

public sealed class CreateCorrectionRequestResponse
{
    public Guid CorrectionRequestId { get; set; }

    public Guid StudentIndicatorId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }
}

public sealed class GetMyCorrectionRequestResponse
{
    public Guid CorrectionRequestId { get; set; }

    public Guid StudentIndicatorId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;

    public string? DecisionNotes { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class CorrectionRequestReviewResponse
{
    public Guid CorrectionRequestId { get; set; }

    public Guid StudentId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public Guid StudentIndicatorId { get; set; }

    public Guid IndicatorId { get; set; }

    public Guid TermId { get; set; }

    public string TermName { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public string? CurrentValue { get; set; }

    public string? ProposedValue { get; set; }

    public string? Explanation { get; set; }

    public string? DecisionNotes { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsShared { get; set; }

    public bool IsErased { get; set; }
}
