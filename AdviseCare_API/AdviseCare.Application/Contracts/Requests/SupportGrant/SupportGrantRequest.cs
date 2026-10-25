using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.SupportGrant
{
    public class SearchSupportGrantRequest
    {
        [Required(ErrorMessage = "Intervention id is required.")]
        public Guid InterventionId { get; set; }
    }

    public class CreateSupportGrantRequest
    {
        [Required(ErrorMessage = "Intervention id is required.")]
        public Guid InterventionId { get; set; }

        public string? GrantDescription { get; set; }

        [Required(ErrorMessage = "Grant type is required."), MaxLength(255,
            ErrorMessage = "Grant type must be under 255 characters.")]
        public string GrantType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issue date is required.")]
        public DateTimeOffset? IssuedAt { get; set; }
    }
}
