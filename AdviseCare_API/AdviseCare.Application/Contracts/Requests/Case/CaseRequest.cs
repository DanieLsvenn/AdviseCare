using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Case
{
    public class SearchCaseRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? SupportCategoryId { get; set; }
        public string SupportType { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Student (WF2): leave StudentId empty; SharedAttributes optional (one per data attribute, each with an optional reason); the priority is always medium.
    // Advisor (EX3): StudentId required; SharedAttributes must stay empty; Priority optional.
    public class CreateCaseRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        [Required(ErrorMessage = "Support category is required.")]
        public Guid SupportCategoryId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Request reason is required.")]
        public string RequestReason { get; set; } = string.Empty;
        public List<CreateCaseSharedAttributeRequest> SharedAttributes { get; set; } = new();
        [RegularExpression("^(low|medium|high|urgent)$",
            ErrorMessage = "Priority must be low, medium, high or urgent.")]
        public string Priority { get; set; } = "medium";
    }

    // One data attribute the student shares with a support request (CASE_ATTRIBUTE), with an optional note on why it matters
    public class CreateCaseSharedAttributeRequest
    {
        [Required(ErrorMessage = "Data attribute is required.")]
        public Guid DataAttributeId { get; set; } = Guid.Empty;
        [StringLength(1000, ErrorMessage = "The note for a shared data attribute cannot exceed 1000 characters.")]
        public string ShareReason { get; set; } = string.Empty;
    }

    public class SearchMyCaseRequest
    {
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SearchAssignedCaseRequest
    {
        // Part of the student's full name or student code
        public string Student { get; set; } = string.Empty;
        public string SupportType { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool UnreadOnly { get; set; } = false;
        [RegularExpression("^(openedAt|priority|stage|studentName)$",
            ErrorMessage = "Sort by must be openedAt, priority, stage or studentName.")]
        public string SortBy { get; set; } = "openedAt";
        [RegularExpression("^(asc|desc)$",
            ErrorMessage = "Sort direction must be asc or desc.")]
        public string SortDirection { get; set; } = "desc";
    }

    public class AssignCaseRequest
    {
        [Required(ErrorMessage = "Advisor is required.")]
        public Guid AdvisorId { get; set; } = Guid.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ReassignCaseRequest
    {
        [Required(ErrorMessage = "Advisor is required.")]
        public Guid AdvisorId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ChangeCaseStageRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
        [Required(ErrorMessage = "Target stage is required."), RegularExpression("^(contacted|followUp|closed)$",
            ErrorMessage = "Target stage must be contacted, followUp or closed; assigned comes from matching, a reopen or an accepted referral, and referred from a referral.")]
        public string ToStage { get; set; } = "contacted";
    }

    public class ReopenCaseRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ChangeCasePriorityRequest
    {
        [Required(ErrorMessage = "Priority is required."), RegularExpression("^(low|medium|high|urgent)$",
            ErrorMessage = "Priority must be low, medium, high or urgent.")]
        public string Priority { get; set; } = "medium";
    }

    public class SearchCaseStageHistoryRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
    }
}
