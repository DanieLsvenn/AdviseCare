using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseChangeRequest
{
    public class SearchCaseChangeRequestRequest
    {
        public Guid? CaseId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateCaseChangeRequestRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Proposed advisor is required.")]
        public Guid ProposedAdvisorId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ApproveCaseChangeRequestRequest
    {
        // Optional note from the Programme Head, stored in decisionNotes
        public string DecisionNotes { get; set; } = string.Empty;
    }

    public class RejectCaseChangeRequestRequest
    {
        // Stored in decisionNotes; a rejected request must have it
        [Required(ErrorMessage = "Decision notes are required when rejecting.")]
        public string DecisionNotes { get; set; } = string.Empty;
    }
}
