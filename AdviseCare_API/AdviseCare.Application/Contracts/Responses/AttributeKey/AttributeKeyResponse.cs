namespace AdviseCare.Application.Contracts.Responses.AttributeKey
{
    public class GetAllAttributeKeyResponse
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DestroyedAt { get; set; }
        public string Status { get; set; } = "active";
    }

    public class GetAllAttributeKeyWithPaginationResponse
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DestroyedAt { get; set; }
        public string Status { get; set; } = "active";
    }

    public class GetAttributeKeyByIdResponse
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DestroyedAt { get; set; }
        public string Status { get; set; } = "active";
    }

    public class SearchAttributeKeyResponse
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DestroyedAt { get; set; }
        public string Status { get; set; } = "active";
    }
}
