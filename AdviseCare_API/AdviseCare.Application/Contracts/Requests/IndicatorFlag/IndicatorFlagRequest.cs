using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.IndicatorFlag
{
    public class SearchIndicatorFlagRequest
    {
        public Guid? IndicatorId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? TermId { get; set; }
        [RegularExpression("^(low|medium|high|urgent)$",
            ErrorMessage = "Priority must be low, medium, high or urgent.")]
        public string Priority { get; set; } = string.Empty;
        [RegularExpression("^(low|moderate|high|critical)$",
            ErrorMessage = "Severity must be low, moderate, high or critical.")]
        public string Severity { get; set; } = string.Empty;
        [RegularExpression("^(academic|wellbeing)$",
            ErrorMessage = "Support type must be academic or wellbeing.")]
        public string SupportType { get; set; } = string.Empty;
        [RegularExpression("^(open|caseOpened|dismissed|cleared)$",
            ErrorMessage = "Status must be open, caseOpened, dismissed or cleared.")]
        public string Status { get; set; } = string.Empty;
    }

    public class DismissIndicatorFlagRequest
    {
        [Required(ErrorMessage = "Dismiss reason is required."), StringLength(1000,
            ErrorMessage = "Dismiss reason cannot exceed 1000 characters.")]
        public string DismissReason { get; set; } = string.Empty;
    }

    // The Programme Head opens a case from a flag for an advisor of the rule's support type; the optional reason goes to the case's stage history
    public class OpenCaseFromIndicatorFlagRequest
    {
        [Required(ErrorMessage = "Advisor is required.")]
        public Guid AdvisorId { get; set; } = Guid.Empty;
        [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
