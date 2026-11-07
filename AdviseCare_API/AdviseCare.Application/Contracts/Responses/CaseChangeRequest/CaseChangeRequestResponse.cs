namespace AdviseCare.Application.Contracts.Responses.CaseChangeRequest
{
    public class GetAllCaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetAllCaseChangeRequestWithPaginationResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class GetCaseChangeRequestByIdResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class SearchCaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class CreateCaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class ApproveCaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }

    public class RejectCaseChangeRequestResponse
    {
        public Guid CaseChangeRequestId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? CurrentAdvisorId { get; set; }
        public Guid? ProgrammeHeadId { get; set; }
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        public Guid RequestedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string CaseCurrentStage { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CurrentAdvisorFullName { get; set; } = string.Empty;
        public string DecisionNotes { get; set; } = string.Empty;
        public string ProgrammeHeadFullName { get; set; } = string.Empty;
        public string ProposedAdvisorFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedByFullName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public DateTime? DecidedAt { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "pending";
    }
}
