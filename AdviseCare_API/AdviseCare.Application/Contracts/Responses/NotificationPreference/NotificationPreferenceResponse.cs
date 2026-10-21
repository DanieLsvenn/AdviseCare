namespace AdviseCare.Application.Contracts.Responses.NotificationPreference
{
    public class GetAllNotificationPreferenceResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class GetAllNotificationPreferenceWithPaginationResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class GetNotificationPreferenceByIdResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class SearchNotificationPreferenceResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class CreateNotificationPreferenceResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateNotificationPreferenceResponse
    {
        public Guid NotificationPreferenceId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class GetMyNotificationPreferenceResponse
    {
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateMyNotificationPreferenceResponse
    {
        public string Channel { get; set; } = "email";
        public bool IsEnabled { get; set; } = true;
    }
}
