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
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "External service id is required.")]
        public Guid ExternalServiceId { get; set; }

        public DateTimeOffset? FollowUpDueAt { get; set; }
    }

    public class RecordExternalReferralFollowUpRequest
    {
        [Required(ErrorMessage = "Follow-up outcome is required.")]
        public string FollowUpOutcome { get; set; } = string.Empty;
    }
}
