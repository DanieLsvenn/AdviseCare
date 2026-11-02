namespace AdviseCare.Application.DTOs
{
    // One value shown to a staff member, written as one ACCESSLOGS row by the access guard
    public class AccessLogEntry
    {
        public Guid StudentId { get; set; } = Guid.Empty;
        // The exact STUDENTDATA row shown; null for reads not tied to one row
        public Guid? StudentDataId { get; set; }
        // Null for a read allowed through an approved emergency access
        public Guid? ConsentPurposeId { get; set; }
        public string AccessedResource { get; set; } = string.Empty;
    }
}
