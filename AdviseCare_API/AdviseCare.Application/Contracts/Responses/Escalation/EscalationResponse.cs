namespace AdviseCare.Application.Contracts.Responses.Escalation
{
    public class EscalationResponse
    {
        public Guid EscalationId { get; set; }
        public Guid CaseId { get; set; }
        public Guid EscalatedBy { get; set; }
        public string EscalatedByName { get; set; } = string.Empty;
        public Guid EscalatedTo { get; set; }
        public string EscalatedToName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime EscalatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
