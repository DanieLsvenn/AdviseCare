using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Referral
{
    public class SearchReferralRequest
    {
        public Guid? CaseId { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateReferralRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Receiver is required.")]
        public Guid ToAccountId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Referral reason is required.")]
        public string ReferralReason { get; set; } = string.Empty;
        [Required(ErrorMessage = "Priority is required."), RegularExpression("^(low|medium|high|urgent)$",
            ErrorMessage = "Priority must be low, medium, high or urgent.")]
        public string Priority { get; set; } = "medium";
    }

    public class SearchMyReferralRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class ReturnReferralRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }
}
