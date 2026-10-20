using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Session
{
    public class SearchSessionRequest
    {
        public Guid? AccountId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateSessionRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
    }

    public class UpdateSessionRequest
    {
        public bool IsActive { get; set; } = true;
    }
}
