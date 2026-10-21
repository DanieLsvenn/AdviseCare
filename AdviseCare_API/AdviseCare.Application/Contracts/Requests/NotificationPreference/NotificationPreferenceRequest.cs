using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.NotificationPreference
{
    public class SearchNotificationPreferenceRequest
    {
        public Guid? AccountId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public bool? IsEnabled { get; set; }
    }

    public class CreateNotificationPreferenceRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Channel is required."), RegularExpression("^(email|inApp)$",
            ErrorMessage = "Channel must be email or inApp.")]
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateNotificationPreferenceRequest
    {
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateMyNotificationPreferenceRequest
    {
        [Required(ErrorMessage = "Channel is required."), RegularExpression("^(email|inApp)$",
            ErrorMessage = "Channel must be email or inApp.")]
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }
}
