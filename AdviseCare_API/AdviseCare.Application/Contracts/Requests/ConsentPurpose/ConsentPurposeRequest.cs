using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.ConsentPurpose
{
    public class SearchConsentPurposeRequest
    {
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateConsentPurposeRequest
    {
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Purpose name is required."), MaxLength(255,
            ErrorMessage = "Purpose name must be under 255 characters.")]
        public string PurposeName { get; set; } = string.Empty;
    }

    public class UpdateConsentPurposeRequest
    {
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Purpose name is required."), MaxLength(255,
            ErrorMessage = "Purpose name must be under 255 characters.")]
        public string PurposeName { get; set; } = string.Empty;
    }

    public class SearchPurposeAttributeRequest
    {
        public Guid? ConsentPurposeId { get; set; }
        public Guid? DataAttributeId { get; set; }
    }

    public class CreatePurposeAttributeRequest
    {
        [Required(ErrorMessage = "Consent purpose Id is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Data attribute Id is required.")]
        public Guid DataAttributeId { get; set; } = Guid.Empty;
    }
}
