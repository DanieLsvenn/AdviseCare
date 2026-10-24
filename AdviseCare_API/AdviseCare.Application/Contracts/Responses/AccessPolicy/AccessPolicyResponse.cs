namespace AdviseCare.Application.Contracts.Responses.AccessPolicy
{
    public class GetAllAccessPolicyResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class GetAllAccessPolicyWithPaginationResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class GetAccessPolicyByIdResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class SearchAccessPolicyResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class CreateAccessPolicyResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class UpdateAccessPolicyResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid CreatedBy { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string CreatorFullName { get; set; } = string.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
    }

    public class TestAccessPolicyResponse
    {
        public Guid AccessPolicyId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string PurposeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public List<TestAccessPolicyAttributeResponse> Attributes { get; set; } = new();
        public bool IsInEffect { get; set; } = false;
        public string PolicyStatus { get; set; } = "draft";
        public string PurposeStatus { get; set; } = "inactive";
    }

    public class TestAccessPolicyAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public bool HasConsent { get; set; } = false;
        public bool HasValue { get; set; } = false;
        public string Result { get; set; } = "notShared";
    }
}
