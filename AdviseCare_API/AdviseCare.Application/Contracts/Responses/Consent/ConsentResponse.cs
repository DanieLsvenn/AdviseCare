namespace AdviseCare.Application.Contracts.Responses.Consent
{
    public class GetAllConsentResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class GetAllConsentWithPaginationResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class GetConsentByIdResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class SearchConsentResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class CreateConsentResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class UpdateConsentResponse
    {
        public Guid ConsentId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public bool IsExpired { get; set; } = false;
        public string Status { get; set; } = "granted";
    }

    public class GetMyConsentResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public List<GetMyConsentAttributeResponse> Attributes { get; set; } = new();
        public string PurposeStatus { get; set; } = "active";
    }

    public class GetMyConsentAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public Guid? ConsentId { get; set; }
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? GrantedAt { get; set; }
        public string AttributeStatus { get; set; } = "active";
        public string Status { get; set; } = "none";
    }

    public class GetAllConsentHistoryResponse
    {
        public Guid ConsentHistoryId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string NewStatus { get; set; } = "granted";
        public string PreviousStatus { get; set; } = string.Empty;
    }

    public class GetAllConsentHistoryWithPaginationResponse
    {
        public Guid ConsentHistoryId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string NewStatus { get; set; } = "granted";
        public string PreviousStatus { get; set; } = string.Empty;
    }

    public class GetConsentHistoryByIdResponse
    {
        public Guid ConsentHistoryId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string NewStatus { get; set; } = "granted";
        public string PreviousStatus { get; set; } = string.Empty;
    }

    public class SearchConsentHistoryResponse
    {
        public Guid ConsentHistoryId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string NewStatus { get; set; } = "granted";
        public string PreviousStatus { get; set; } = string.Empty;
    }

    public class GetMyConsentHistoryResponse
    {
        public Guid ConsentHistoryId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public Guid ConsentId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string NewStatus { get; set; } = "granted";
        public string PreviousStatus { get; set; } = string.Empty;
    }
}
