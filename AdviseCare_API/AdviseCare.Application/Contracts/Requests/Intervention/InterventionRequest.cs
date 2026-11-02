using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Intervention
{
    public class SearchInterventionRequest
    {
        public Guid? CaseId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateInterventionRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        public string Notes { get; set; } = string.Empty;
        [Required(ErrorMessage = "Intervention date is required.")]
        public DateTime InterventionDate { get; set; }
        [Required(ErrorMessage = "Intervention type is required."), RegularExpression("^(loanArrangement|financialAid|bankingPaperwork|other)$",
            ErrorMessage = "Intervention type must be loanArrangement, financialAid, bankingPaperwork or other.")]
        public string InterventionType { get; set; } = "loanArrangement";
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(scheduled|inProgress)$",
            ErrorMessage = "Status must be scheduled or inProgress; use the close action to complete an intervention.")]
        public string Status { get; set; } = "scheduled";
    }

    public class UpdateInterventionRequest
    {
        public string Notes { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        [Required(ErrorMessage = "Intervention date is required.")]
        public DateTime InterventionDate { get; set; }
        [Required(ErrorMessage = "Intervention type is required."), RegularExpression("^(loanArrangement|financialAid|bankingPaperwork|other)$",
            ErrorMessage = "Intervention type must be loanArrangement, financialAid, bankingPaperwork or other.")]
        public string InterventionType { get; set; } = "loanArrangement";
        [Required(ErrorMessage = "Status is required."), RegularExpression("^(scheduled|inProgress)$",
            ErrorMessage = "Status must be scheduled or inProgress; use the close action to complete an intervention.")]
        public string Status { get; set; } = "scheduled";
    }

    public class CloseInterventionRequest
    {
        [Required(ErrorMessage = "Outcome is required.")]
        public string Outcome { get; set; } = string.Empty;
    }
}
