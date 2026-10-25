namespace AdviseCare.Application.Contracts.Responses.ConsentPurpose
{
    public class GetAllConsentPurposeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
    }

    public class GetAllConsentPurposeWithPaginationResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
    }

    public class GetConsentPurposeByIdResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public List<ConsentPurposeAttributeResponse> DataAttributes { get; set; } = new();
        public string Status { get; set; } = "inactive";
    }

    public class ConsentPurposeAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
    }

    public class SearchConsentPurposeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
    }

    public class CreateConsentPurposeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
    }

    public class UpdateConsentPurposeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
    }

    public class GetAllPurposeAttributeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string AttributeStatus { get; set; } = "active";
        public string PurposeStatus { get; set; } = "inactive";
    }

    public class GetAllPurposeAttributeWithPaginationResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string AttributeStatus { get; set; } = "active";
        public string PurposeStatus { get; set; } = "inactive";
    }

    public class GetPurposeAttributeByIdResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string AttributeStatus { get; set; } = "active";
        public string PurposeStatus { get; set; } = "inactive";
    }

    public class SearchPurposeAttributeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string AttributeStatus { get; set; } = "active";
        public string PurposeStatus { get; set; } = "inactive";
    }

    public class CreatePurposeAttributeResponse
    {
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string AttributeStatus { get; set; } = "active";
        public string PurposeStatus { get; set; } = "inactive";
    }
}
