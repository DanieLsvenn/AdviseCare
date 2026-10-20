namespace AdviseCare.Application.Contracts.Responses.Session
{
    public class GetAllSessionResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GetAllSessionWithPaginationResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GetSessionByIdResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SearchSessionResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateSessionResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSessionResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public Guid AccountId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GetMySessionResponse
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCurrent { get; set; } = false;
    }

    public class LogoutResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
