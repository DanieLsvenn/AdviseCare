using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Notification
{
    public class SearchNotificationRequest
    {
        public Guid? AccountId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool? IsRead { get; set; }
    }

    public class CreateNotificationRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        [Required(ErrorMessage = "Title is required."), MaxLength(255,
            ErrorMessage = "Title must be under 255 characters.")]
        public string Title { get; set; } = string.Empty;
    }

    public class UpdateNotificationRequest
    {
        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        [Required(ErrorMessage = "Title is required."), MaxLength(255,
            ErrorMessage = "Title must be under 255 characters.")]
        public string Title { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
    }
}
