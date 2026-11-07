namespace AdviseCare.Application.Contracts.Responses.SupportCategory
{
    public class GetAllSupportCategoryResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class GetAllSupportCategoryWithPaginationResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class GetSupportCategoryByIdResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class SearchSupportCategoryResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class CreateSupportCategoryResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class UpdateSupportCategoryResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public string SupportType { get; set; } = "academic";
        public string Status { get; set; } = "active";
    }

    public class GetSupportCategoryShareableAttributesResponse
    {
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public List<SupportCategoryShareableAttributeResponse> Attributes { get; set; } = new();
        public bool CanShare { get; set; } = false;
    }

    public class SupportCategoryShareableAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public bool AlreadyShared { get; set; } = false;
    }
}
