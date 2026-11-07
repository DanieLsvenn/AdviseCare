namespace AdviseCare.Application.Contracts.Responses.Case
{
    public class GetAllCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetAllCaseWithPaginationResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetCaseByIdResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
        public List<CaseSharedAttributeResponse> SharedAttributes { get; set; } = new();
    }

    // One data attribute the student shared with the request (CASE_ATTRIBUTE) and the student's note about it
    public class CaseSharedAttributeResponse
    {
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string ShareReason { get; set; } = string.Empty;
        public DateTime SharedAt { get; set; }
    }

    public class SearchCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class CreateCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
        public List<CaseSharedAttributeResponse> SharedAttributes { get; set; } = new();
    }

    public class GetMyCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetMatchingQueueCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetAssignedCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public int UnreadNotificationCount { get; set; } = 0;
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetAssignedCaseWithPaginationResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid ConsentPurposeId { get; set; } = Guid.Empty;
        public Guid OpenedBy { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OpenedByFullName { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public DateTime OpenedAt { get; set; }
        public int UnreadNotificationCount { get; set; } = 0;
        public string SupportType { get; set; } = "academic";
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class GetCaseSuggestedAdvisorResponse
    {
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> MatchReasons { get; set; } = new();
        public List<string> SpecialisedSubjectCodes { get; set; } = new();
        public int OpenCaseCount { get; set; } = 0;
        public bool IsFamiliarAdvisor { get; set; } = false;
        public bool IsGeneralAdvisor { get; set; } = false;
        public bool IsSameFaculty { get; set; } = false;
        public bool IsSubjectSpecialist { get; set; } = false;
    }

    public class AssignCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class ReassignCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class ChangeCaseStageResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class ReopenCaseResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class ChangeCasePriorityResponse
    {
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid? AdvisorId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open";
    }

    public class SearchCaseStageHistoryResponse
    {
        public Guid CaseStageHistoryId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid ChangedBy { get; set; } = Guid.Empty;
        public string ChangedByFullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string FromStage { get; set; } = "pending";
        public string ToStage { get; set; } = "pending";
    }
}
