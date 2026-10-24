using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.ConsentRequest
{
    public class SearchConsentRequestRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? ConsentPurposeId { get; set; }
        public Guid? DataAttributeId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateConsentRequestRequest
    {
        [Required(ErrorMessage = "Consent purpose Id is required.")]
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Data attribute Id is required.")]
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Student Id is required.")]
        public Guid StudentId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "The purpose of the request is required.")]
        public string PurposeStated { get; set; } = string.Empty;
    }

    public class UpdateConsentRequestRequest
    {
        [Required(ErrorMessage = "The purpose of the request is required.")]
        public string PurposeStated { get; set; } = string.Empty;
    }
}
