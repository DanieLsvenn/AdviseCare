namespace AdviseCare.Application.DTOs
{
    // Result of the access guard for one student and one data attribute
    public class AccessDecision
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        // The purpose the access is allowed under (null when refused, or when students read their own data)
        public Guid? ConsentPurposeId { get; set; }
        public bool IsAllowed { get; set; } = false;
    }
}
