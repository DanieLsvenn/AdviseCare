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
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Programme Head is required.")]
        public Guid EscalatedTo { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class RespondEscalationRequest
    {
        [Required(ErrorMessage = "Response is required.")]
        public string Response { get; set; } = string.Empty;
    }
}
