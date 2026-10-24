using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Consent
{
    public class SearchConsentRequest
    {
        public Guid? ConsentPurposeId { get; set; }
        public Guid? DataAttributeId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateConsentRequest
    {
        [Required(ErrorMessage = "Consent purpose Id is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "At least one data attribute is required."), MinLength(1,
            ErrorMessage = "At least one data attribute is required.")]
        public List<Guid> DataAttributeIds { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }

    public class UpdateConsentRequest
    {
        public DateTime? ExpiresAt { get; set; }
    }

    public class SearchConsentHistoryRequest
    {
        public Guid? ChangedBy { get; set; }
        public Guid? ConsentId { get; set; }
        public Guid? StudentId { get; set; }
    }
}
