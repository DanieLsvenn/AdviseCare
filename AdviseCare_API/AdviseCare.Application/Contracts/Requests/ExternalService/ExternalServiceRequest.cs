using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.ExternalService
{
    public class SearchExternalServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }

    public class CreateExternalServiceRequest
    {
        public string ContactInfo { get; set; } = string.Empty;
        [Required(ErrorMessage = "Service name is required."), MaxLength(255,
            ErrorMessage = "Service name must be under 255 characters.")]
        public string ServiceName { get; set; } = string.Empty;
        [MaxLength(255, ErrorMessage = "Service type must be under 255 characters.")]
        public string ServiceType { get; set; } = string.Empty;
    }

    public class UpdateExternalServiceRequest
    {
        public string ContactInfo { get; set; } = string.Empty;
        [Required(ErrorMessage = "Service name is required."), MaxLength(255,
            ErrorMessage = "Service name must be under 255 characters.")]
        public string ServiceName { get; set; } = string.Empty;
        [MaxLength(255, ErrorMessage = "Service type must be under 255 characters.")]
        public string ServiceType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
