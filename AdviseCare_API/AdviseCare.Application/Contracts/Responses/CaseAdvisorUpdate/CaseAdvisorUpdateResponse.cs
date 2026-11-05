namespace AdviseCare.Application.Contracts.Responses.CaseAdvisorUpdate
{
    public class CaseAdvisorUpdateResponse
    {
        public Guid CaseAdvisorUpdateId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
