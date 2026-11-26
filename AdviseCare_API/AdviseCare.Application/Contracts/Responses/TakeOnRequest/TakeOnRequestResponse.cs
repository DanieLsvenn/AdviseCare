namespace AdviseCare.Application.Contracts.Responses.TakeOnRequest
{
    // A take-on request with the item it asks for (targetType case or flag): the student's directory data, category, subject, term (flag),
    // rule name (flag), severity and priority of the item; advisorOpenCaseCount = the advisor's cases that are not closed (for the Programme Head)
    public class GetAllTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class GetAllTakeOnRequestWithPaginationResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class GetTakeOnRequestByIdResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class SearchTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class CreateTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class UpdateTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class GetMyTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class ApproveTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class DeclineTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }

    public class WithdrawTakeOnRequestResponse
    {
        public Guid TakeOnRequestId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? DecidedBy { get; set; }
        public Guid? IndicatorFlagId { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DecidedByFullName { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public int AdvisorOpenCaseCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Severity { get; set; }
        public string SupportType { get; set; } = "academic";
        public string TargetType { get; set; } = "case";
        public string Status { get; set; } = "pending";
    }
}
