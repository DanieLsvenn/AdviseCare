namespace AdviseCare.Application.Contracts.Responses.CaseChangeRequest
{
    public class CaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; }
        public Guid CaseId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        // The case's advisor now (after approval: the proposed advisor)
        public Guid? CurrentAdvisorId { get; set; }
        public string CurrentAdvisorName { get; set; } = string.Empty;
        public Guid ProposedAdvisorId { get; set; }
        public string ProposedAdvisorName { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public Guid? ProgrammeHeadId { get; set; }
        public string ProgrammeHeadName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
