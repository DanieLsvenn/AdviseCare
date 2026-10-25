using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CorrectionRequest;

public sealed class CreateCorrectionRequest
{
    [Required]
    public Guid StudentIndicatorId { get; set; }

    [Required]
    public string ProposedValue { get; set; } = string.Empty;

    [Required]
    public string Explanation { get; set; } = string.Empty;
}

public sealed class CorrectionDecisionRequest
{
    [Required]
    public string DecisionNotes { get; set; } = string.Empty;
}
