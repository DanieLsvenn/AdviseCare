namespace AdviseCare.Application.DTOs
{
    public sealed record AccessLogBatchEntry(
        Guid StudentId,
        Guid ConsentPurposeId,
        string AccessedResource);

    // Result of the access guard for one student and one data attribute
    public class AccessDecision
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        // The purpose the access is allowed under (null when refused, for an emergency access, or when students read their own data)
        public Guid? ConsentPurposeId { get; set; }
        public bool IsAllowed { get; set; } = false;
        // True when the access is allowed only through an approved, unexpired emergency access request
        public bool IsEmergencyAccess { get; set; } = false;
    }
}
