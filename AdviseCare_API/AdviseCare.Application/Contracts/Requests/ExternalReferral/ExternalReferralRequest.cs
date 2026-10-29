using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.ExternalReferral
{
    public class SearchExternalReferralRequest
    {
        public Guid? CaseId { get; set; }
        public Guid? ExternalServiceId { get; set; }
        public bool? IsOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateExternalReferralRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "External service is required.")]
        public Guid ExternalServiceId { get; set; } = Guid.Empty;
        public DateTime? FollowUpDueAt { get; set; }
    }

    public class UpdateExternalReferralRequest
    {
        [Required(ErrorMessage = "Follow-up outcome is required.")]
        public string FollowUpOutcome { get; set; } = string.Empty;
    }
}
