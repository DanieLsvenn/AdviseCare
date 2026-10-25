namespace AdviseCare.Application.Contracts.Responses.EmergencyAccessRequest
{
    public class GetAllEmergencyAccessRequestResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetAllEmergencyAccessRequestWithPaginationResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetEmergencyAccessRequestByIdResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class SearchEmergencyAccessRequestResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class CreateEmergencyAccessRequestResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class UpdateEmergencyAccessRequestResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }

    public class GetMyEmergencyAccessRequestResponse
    {
        public Guid EmergencyAccessRequestId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid? StewardId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string RequesterFullName { get; set; } = string.Empty;
        public string StewardFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ScopeExpiresAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "pending";
    }
}
