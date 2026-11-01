using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Case
{
    // POST /api/Cases
    // Student (WF2): studentId empty, sharedAttributeIds optional, priority ignored (always medium).
    // Advisor outreach (EX3): studentId required, sharedAttributeIds must be empty, priority optional.
    public class CreateCaseRequest
    {
        public Guid? StudentId { get; set; }

        public Guid? SubjectId { get; set; }

        [Required(ErrorMessage = "Support category id is required.")]
        public Guid SupportCategoryId { get; set; }

        [Required(ErrorMessage = "Request reason is required.")]
        [MaxLength(4000, ErrorMessage = "Request reason must be at most 4000 characters.")]
        public string RequestReason { get; set; } = string.Empty;

        public string? Priority { get; set; }

        public List<Guid> SharedAttributeIds { get; set; } = new();
    }

    public class SearchMyCasesRequest
    {
        public string Status { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
    }

    public class SearchAssignedCasesRequest
    {
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        // Part of the student's full name or student code
        public string Student { get; set; } = string.Empty;
        public bool? UnreadOnly { get; set; }
        // openedAt (default), priority, stage or studentName
        public string SortBy { get; set; } = "openedAt";
        // asc or desc (default)
        public string SortDirection { get; set; } = "desc";
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class AssignCaseRequest
    {
        [Required(ErrorMessage = "Advisor id is required.")]
        public Guid AdvisorId { get; set; }

        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string? Reason { get; set; }
    }

    public class ReassignCaseRequest
    {
        [Required(ErrorMessage = "Advisor id is required.")]
        public Guid AdvisorId { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ChangeCaseStageRequest
    {
        [Required(ErrorMessage = "Target stage is required.")]
        public string ToStage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReopenCaseRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ChangeCasePriorityRequest
    {
        [Required(ErrorMessage = "Priority is required (low, medium, high or urgent).")]
        public string Priority { get; set; } = string.Empty;
    }

    public class SearchCaseStageHistoryRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }
    }
}
