namespace AdviseCare.Application.Contracts.Responses.Notification
{
    public class GetAllNotificationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class GetAllNotificationWithPaginationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class GetNotificationByIdResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class SearchNotificationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class CreateNotificationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class UpdateNotificationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class GetMyNotificationResponse
    {
        public Guid NotificationId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
