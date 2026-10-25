using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Escalation
{
    public class SearchEscalationRequest
    {
        public Guid? CaseId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateEscalationRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "Programme Head id is required.")]
        public Guid EscalatedTo { get; set; }

        [Required(ErrorMessage = "Escalation reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class RespondToEscalationRequest
    {
        [Required(ErrorMessage = "Response is required.")]
        public string Response { get; set; } = string.Empty;
    }
}
