namespace AdviseCare.Application.Contracts.Responses.SupportCategory
{
    public class SupportCategoryResponse
    {
        public Guid SupportCategoryId { get; set; }
        public Guid ConsentPurposeId { get; set; }
        public string PurposeName { get; set; } = string.Empty;
        public string PurposeStatus { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // What a student may share when requesting support in this category (D2.2):
    // the active data attributes of the category's governing consent purpose
    public class ShareableAttributesResponse
    {
        public Guid SupportCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid ConsentPurposeId { get; set; }
        public string PurposeName { get; set; } = string.Empty;
        // False when the purpose is not active: the student can still submit the request, but cannot share anything with it
        public bool CanShare { get; set; }
        public List<ShareableAttributeItem> Attributes { get; set; } = new();
    }

    public class ShareableAttributeItem
    {
        public Guid DataAttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDescription { get; set; } = string.Empty;
        // Only filled for a Student caller: true when a granted, unexpired consent for this purpose and attribute already exists
        public bool AlreadyShared { get; set; }
    }
}
