namespace AdviseCare.Application.Contracts.Responses.AttributeKey
{
    public class GetAllAttributeKeyResponse
    {
        public Guid AttributeKeyId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public int MasterKeyVersion { get; set; } = 0;
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
        public int MasterKeyVersion { get; set; } = 0;
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
        public int MasterKeyVersion { get; set; } = 0;
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
        public int MasterKeyVersion { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DestroyedAt { get; set; }
        public string Status { get; set; } = "active";
    }

    public class RewrapAttributeKeyResponse
    {
        public int CurrentMasterKeyVersion { get; set; } = 0;
        public int RewrappedCount { get; set; } = 0;
        public List<int> ConfiguredVersions { get; set; } = new();
        // Versions no active key uses any more; one of them may be removed from the configuration once no kept database backup needs it
        public List<int> UnusedVersions { get; set; } = new();
    }
}
