using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.SupportGrant
{
    public class SearchSupportGrantRequest
    {
        public Guid? InterventionId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSupportGrantRequest
    {
        [Required(ErrorMessage = "Intervention is required.")]
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = "Grant type is required."), MaxLength(255,
            ErrorMessage = "Grant type must be under 255 characters.")]
        public string GrantType { get; set; } = string.Empty;
        [Required(ErrorMessage = "Issue date is required.")]
        public DateTime IssuedAt { get; set; }
    }
}
