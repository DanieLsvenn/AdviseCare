namespace AdviseCare.Application.Contracts.Responses.SupportGrant
{
    public class SupportGrantResponse
    {
        public Guid SupportGrantId { get; set; }
        public Guid InterventionId { get; set; }
        public string GrantDescription { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
