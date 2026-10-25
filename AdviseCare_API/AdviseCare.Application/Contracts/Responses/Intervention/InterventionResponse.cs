namespace AdviseCare.Application.Contracts.Responses.Intervention
{
    public class InterventionResponse
    {
        public Guid InterventionId { get; set; }
        public Guid CaseId { get; set; }
        public Guid StudentId { get; set; }
        public Guid OfficerId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime InterventionDate { get; set; }
        public string InterventionType { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
