using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Intervention
{
    public class SearchInterventionRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? CaseId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateInterventionRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "Intervention date is required.")]
        public DateTimeOffset? InterventionDate { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Intervention type is required."), RegularExpression(
            "^(loanArrangement|financialAid|bankingPaperwork|other)$",
            ErrorMessage = "Intervention type must be loanArrangement, financialAid, bankingPaperwork, or other.")]
        public string InterventionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required."), RegularExpression(
            "^(scheduled|inProgress)$",
            ErrorMessage = "Status must be scheduled or inProgress. Use the close action to complete an intervention.")]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateInterventionRequest
    {
        [Required(ErrorMessage = "Intervention date is required.")]
        public DateTimeOffset? InterventionDate { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Intervention type is required."), RegularExpression(
            "^(loanArrangement|financialAid|bankingPaperwork|other)$",
            ErrorMessage = "Intervention type must be loanArrangement, financialAid, bankingPaperwork, or other.")]
        public string InterventionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required."), RegularExpression(
            "^(scheduled|inProgress)$",
            ErrorMessage = "Status must be scheduled or inProgress. Use the close action to complete an intervention.")]
        public string Status { get; set; } = string.Empty;
    }

    public class RecordInterventionOutcomeRequest
    {
        [Required(ErrorMessage = "Outcome is required.")]
        public string Outcome { get; set; } = string.Empty;
    }

    public class CloseInterventionRequest
    {
        [Required(ErrorMessage = "Outcome is required.")]
        public string Outcome { get; set; } = string.Empty;
    }
}
