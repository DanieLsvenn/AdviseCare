namespace AdviseCare.Application.Contracts.Responses.ExternalReferral
{
    public class ExternalReferralResponse
    {
        public Guid ExternalReferralId { get; set; }
        public Guid CaseId { get; set; }
        public Guid ExternalServiceId { get; set; }
        public string ExternalServiceName { get; set; } = string.Empty;
        public Guid ReferredBy { get; set; }
        public DateTime? FollowUpDueAt { get; set; }
        public string FollowUpOutcome { get; set; } = string.Empty;
        public DateTime ReferredAt { get; set; }
        public bool IsOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
