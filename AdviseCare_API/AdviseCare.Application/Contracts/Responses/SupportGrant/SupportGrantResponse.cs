namespace AdviseCare.Application.Contracts.Responses.SupportGrant
{
    public class GetAllSupportGrantResponse
    {
        public Guid SupportGrantId { get; set; } = Guid.Empty;
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
    }

    public class GetAllSupportGrantWithPaginationResponse
    {
        public Guid SupportGrantId { get; set; } = Guid.Empty;
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
    }

    public class GetSupportGrantByIdResponse
    {
        public Guid SupportGrantId { get; set; } = Guid.Empty;
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
    }

    public class SearchSupportGrantResponse
    {
        public Guid SupportGrantId { get; set; } = Guid.Empty;
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
    }

    public class CreateSupportGrantResponse
    {
        public Guid SupportGrantId { get; set; } = Guid.Empty;
        public Guid InterventionId { get; set; } = Guid.Empty;
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
    }
}
