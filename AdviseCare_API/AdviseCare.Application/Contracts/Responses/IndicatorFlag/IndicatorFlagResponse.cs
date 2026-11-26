namespace AdviseCare.Application.Contracts.Responses.IndicatorFlag
{
    // A flag shows the student's directory data, the rule (name, description, data attribute, operator and threshold), its severity, priority,
    // term and subject, never the student's value. PendingTakeOnRequestCount is filled for the Programme Head; MyTakeOnRequest* for an advisor
    public class GetAllIndicatorFlagResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid? MyTakeOnRequestId { get; set; }
        public Guid? ResolvedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DismissReason { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string IndicatorDescription { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string MyTakeOnRequestStatus { get; set; } = string.Empty;
        public string ResolvedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string ThresholdValue { get; set; } = string.Empty;
        public int PendingTakeOnRequestCount { get; set; } = 0;
        public DateTime FlaggedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ComparisonOperator { get; set; } = "lessThan";
        public string SupportType { get; set; } = "academic";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "open";
    }

    public class GetAllIndicatorFlagWithPaginationResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid? MyTakeOnRequestId { get; set; }
        public Guid? ResolvedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DismissReason { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string IndicatorDescription { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string MyTakeOnRequestStatus { get; set; } = string.Empty;
        public string ResolvedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string ThresholdValue { get; set; } = string.Empty;
        public int PendingTakeOnRequestCount { get; set; } = 0;
        public DateTime FlaggedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ComparisonOperator { get; set; } = "lessThan";
        public string SupportType { get; set; } = "academic";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "open";
    }

    public class GetIndicatorFlagByIdResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid? MyTakeOnRequestId { get; set; }
        public Guid? ResolvedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DismissReason { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string IndicatorDescription { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string MyTakeOnRequestStatus { get; set; } = string.Empty;
        public string ResolvedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string ThresholdValue { get; set; } = string.Empty;
        public int PendingTakeOnRequestCount { get; set; } = 0;
        public DateTime FlaggedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ComparisonOperator { get; set; } = "lessThan";
        public string SupportType { get; set; } = "academic";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "open";
    }

    public class SearchIndicatorFlagResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid? MyTakeOnRequestId { get; set; }
        public Guid? ResolvedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DismissReason { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string IndicatorDescription { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string MyTakeOnRequestStatus { get; set; } = string.Empty;
        public string ResolvedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string ThresholdValue { get; set; } = string.Empty;
        public int PendingTakeOnRequestCount { get; set; } = 0;
        public DateTime FlaggedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ComparisonOperator { get; set; } = "lessThan";
        public string SupportType { get; set; } = "academic";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "open";
    }

    public class DismissIndicatorFlagResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid? CaseId { get; set; }
        public Guid? FacultyId { get; set; }
        public Guid IndicatorId { get; set; } = Guid.Empty;
        public Guid? MyTakeOnRequestId { get; set; }
        public Guid? ResolvedBy { get; set; }
        public Guid StudentId { get; set; } = Guid.Empty;
        public Guid? SubjectId { get; set; }
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        public Guid TermId { get; set; } = Guid.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DismissReason { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string IndicatorDescription { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string MyTakeOnRequestStatus { get; set; } = string.Empty;
        public string ResolvedByFullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public string ThresholdValue { get; set; } = string.Empty;
        public int PendingTakeOnRequestCount { get; set; } = 0;
        public DateTime FlaggedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ComparisonOperator { get; set; } = "lessThan";
        public string SupportType { get; set; } = "academic";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "open";
    }

    public class CheckIndicatorFlagResponse
    {
        public int CheckedStudentCount { get; set; } = 0;
        public int CheckedValueCount { get; set; } = 0;
        public int ClearedCount { get; set; } = 0;
        public int RaisedCount { get; set; } = 0;
        public DateTime CheckedAt { get; set; }
    }

    public class OpenCaseFromIndicatorFlagResponse
    {
        public Guid IndicatorFlagId { get; set; } = Guid.Empty;
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string AdvisorFullName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string IndicatorName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public string CurrentStage { get; set; } = "assigned";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "moderate";
        public string Status { get; set; } = "caseOpened";
    }
}
