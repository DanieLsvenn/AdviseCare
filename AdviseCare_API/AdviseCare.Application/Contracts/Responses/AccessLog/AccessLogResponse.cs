namespace AdviseCare.Application.Contracts.Responses.AccessLog
{
    public class GetAllAccessLogResponse
    {
        public Guid AccessLogId { get; set; } = Guid.Empty;
        public Guid AccessorId { get; set; } = Guid.Empty;
        public Guid? ConsentPurposeId { get; set; }
        public Guid RecordedUserId { get; set; } = Guid.Empty;
        public string AccessorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RecordedUserFullName { get; set; } = string.Empty;
        public string AccessedResource { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class GetAllAccessLogWithPaginationResponse
    {
        public Guid AccessLogId { get; set; } = Guid.Empty;
        public Guid AccessorId { get; set; } = Guid.Empty;
        public Guid? ConsentPurposeId { get; set; }
        public Guid RecordedUserId { get; set; } = Guid.Empty;
        public string AccessorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RecordedUserFullName { get; set; } = string.Empty;
        public string AccessedResource { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class GetAccessLogByIdResponse
    {
        public Guid AccessLogId { get; set; } = Guid.Empty;
        public Guid AccessorId { get; set; } = Guid.Empty;
        public Guid? ConsentPurposeId { get; set; }
        public Guid RecordedUserId { get; set; } = Guid.Empty;
        public string AccessorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RecordedUserFullName { get; set; } = string.Empty;
        public string AccessedResource { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class SearchAccessLogResponse
    {
        public Guid AccessLogId { get; set; } = Guid.Empty;
        public Guid AccessorId { get; set; } = Guid.Empty;
        public Guid? ConsentPurposeId { get; set; }
        public Guid RecordedUserId { get; set; } = Guid.Empty;
        public string AccessorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RecordedUserFullName { get; set; } = string.Empty;
        public string AccessedResource { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class GetMyAccessLogResponse
    {
        public Guid AccessLogId { get; set; } = Guid.Empty;
        public Guid? ConsentPurposeId { get; set; }
        public string AccessorFullName { get; set; } = string.Empty;
        public List<string> AccessorRoleNames { get; set; } = new();
        public string PurposeName { get; set; } = string.Empty;
        public string AccessedResource { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string Status { get; set; } = string.Empty;
    }

    public class ExportProcessingRecordResponse
    {
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
