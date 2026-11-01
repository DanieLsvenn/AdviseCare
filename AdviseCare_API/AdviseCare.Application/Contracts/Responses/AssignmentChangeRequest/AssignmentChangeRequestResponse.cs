using AdviseCare.Application.Contracts.Responses.AdvisorAssignment;

namespace AdviseCare.Application.Contracts.Responses.AssignmentChangeRequest
{
    public class AssignmentChangeRequestResponse
    {
        public Guid AssignmentChangeRequestId { get; set; }
        public Guid AdvisorAssignmentId { get; set; }
        public Guid CurrentAdvisorId { get; set; }
        public string CurrentAdvisorName { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public Guid ProposedAdvisorId { get; set; }
        public string ProposedAdvisorName { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public Guid? ProgrammeHeadId { get; set; }
        public string ProgrammeHeadName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        // Only filled in the approve response
        public AdvisorHandoverResponse? Handover { get; set; }
    }
}
