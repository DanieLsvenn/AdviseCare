using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.EmergencyAccessRequest
{
    public class SearchEmergencyAccessRequestRequest
    {
        public Guid? RequestedBy { get; set; }
        public Guid? StewardId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateEmergencyAccessRequestRequest
    {
        [Required(ErrorMessage = "Student Id is required.")]
        public Guid StudentId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Justification is required.")]
        public string Justification { get; set; } = string.Empty;
    }

    public class UpdateEmergencyAccessRequestRequest
    {
        [Required(ErrorMessage = "Justification is required.")]
        public string Justification { get; set; } = string.Empty;
    }

    public class ApproveEmergencyAccessRequestRequest
    {
        [Required(ErrorMessage = "The end of the access window is required.")]
        public DateTime? ScopeExpiresAt { get; set; }
    }

    public class RejectEmergencyAccessRequestRequest
    {
        [Required(ErrorMessage = "The reason for rejecting is required.")]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
