namespace AdviseCare.Application.Contracts.Responses.ActivityLog
{
    public class GetAllActivityLogResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class GetAllActivityLogWithPaginationResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class GetActivityLogByIdResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class SearchActivityLogResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class CreateActivityLogResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class UpdateActivityLogResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class GetMyActivityLogResponse
    {
        public Guid ActivityLogId { get; set; } = Guid.Empty;
        public string ActivityDetail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
