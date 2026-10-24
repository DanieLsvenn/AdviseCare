namespace AdviseCare.Application.Contracts.Responses.ErasureRequest
{
    public class GetAllErasureRequestResponse
    {
        public Guid ErasureRequestId { get; set; } = Guid.Empty;
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid? ExecutedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ExecutedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExecutedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string KeyStatus { get; set; } = "active";
        public string Status { get; set; } = "pending";
    }

    public class GetAllErasureRequestWithPaginationResponse
    {
        public Guid ErasureRequestId { get; set; } = Guid.Empty;
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid? ExecutedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ExecutedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExecutedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string KeyStatus { get; set; } = "active";
        public string Status { get; set; } = "pending";
    }

    public class GetErasureRequestByIdResponse
    {
        public Guid ErasureRequestId { get; set; } = Guid.Empty;
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid? ExecutedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ExecutedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExecutedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string KeyStatus { get; set; } = "active";
        public string Status { get; set; } = "pending";
    }

    public class SearchErasureRequestResponse
    {
        public Guid ErasureRequestId { get; set; } = Guid.Empty;
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid? ExecutedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ExecutedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExecutedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string KeyStatus { get; set; } = "active";
        public string Status { get; set; } = "pending";
    }
}
