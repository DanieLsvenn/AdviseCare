namespace AdviseCare.Application.Contracts.Responses.Case
{
    public class CaseResponse
    {
        public Guid CaseId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public Guid? AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
        // The advisor's contact information, shown to the student once the case is matched (WF2 step 4.1)
        public string AdvisorEmail { get; set; } = string.Empty;
        // Active ADVISORASSIGNMENTS row of the case's advisor and student, used for EX6 change requests
        public Guid? AdvisorAssignmentId { get; set; }
        public Guid OpenedBy { get; set; }
        public string OpenedByName { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public Guid SupportCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public Guid ConsentPurposeId { get; set; }
        public string RequestReason { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // Only filled in the advisor's assigned list: unread notifications of the signed-in advisor that point to this case
        public int UnreadNotificationCount { get; set; }
    }

    public class AssignedCasesResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        // Assigned cases (after filters) that have at least one unread notification for the advisor
        public int UnreadCaseCount { get; set; }
        public List<CaseResponse> Items { get; set; } = new();
    }

    public class SuggestedAdvisorResponse
    {
        public Guid AdvisorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public List<string> SpecialisedSubjectCodes { get; set; } = new();
        // Specialised in the case's subject (ACCOUNT_SUBJECT)
        public bool IsSubjectSpecialist { get; set; }
        // Academic advisor with no subject link
        public bool IsGeneralAdvisor { get; set; }
        public bool IsSameFaculty { get; set; }
        // Already has an active ADVISORASSIGNMENTS row with this student (familiar pair)
        public bool IsFamiliarAdvisor { get; set; }
        // Cases of this advisor that are not closed (workload)
        public int OpenCaseCount { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }

    public class CaseStageHistoryResponse
    {
        public Guid CaseStageHistoryId { get; set; }
        public Guid CaseId { get; set; }
        public Guid ChangedBy { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string FromStage { get; set; } = string.Empty;
        public string ToStage { get; set; } = string.Empty;
    }
}
