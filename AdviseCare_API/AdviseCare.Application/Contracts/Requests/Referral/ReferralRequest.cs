using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Referral
{
    public class CreateReferralRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "Receiver account id is required.")]
        public Guid ToAccountId { get; set; }

        [Required(ErrorMessage = "Referral reason is required.")]
        [MaxLength(4000, ErrorMessage = "Referral reason must be at most 4000 characters.")]
        public string ReferralReason { get; set; } = string.Empty;

        // low, medium (default), high or urgent
        public string Priority { get; set; } = "medium";
    }

    public class SearchMyReferralsRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class SearchReferralRequest
    {
        public Guid? CaseId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReturnReferralRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
