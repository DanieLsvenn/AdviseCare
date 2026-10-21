using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.ActivityLog
{
    public class SearchActivityLogRequest
    {
        public Guid? AccountId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
    }

    public class CreateActivityLogRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        [Required(ErrorMessage = "Activity type is required."), MaxLength(255,
            ErrorMessage = "Activity type must be under 255 characters.")]
        public string ActivityType { get; set; } = string.Empty;
    }

    public class UpdateActivityLogRequest
    {
        public string ActivityDetail { get; set; } = string.Empty;
        [Required(ErrorMessage = "Activity type is required."), MaxLength(255,
            ErrorMessage = "Activity type must be under 255 characters.")]
        public string ActivityType { get; set; } = string.Empty;
    }
}
