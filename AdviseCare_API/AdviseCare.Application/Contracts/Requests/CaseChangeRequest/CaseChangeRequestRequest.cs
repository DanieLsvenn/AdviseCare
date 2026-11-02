using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseChangeRequest
{
    public class CreateCaseChangeRequestRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "Proposed advisor id is required.")]
        public Guid ProposedAdvisorId { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(4000, ErrorMessage = "Reason must be at most 4000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class SearchCaseChangeRequestRequest
    {
        public Guid? CaseId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ApproveCaseChangeRequestRequest
    {
        // Optional note from the Programme Head, stored in decisionNotes
        [MaxLength(2000, ErrorMessage = "Decision notes must be at most 2000 characters.")]
        public string? DecisionNotes { get; set; }
    }

    public class RejectCaseChangeRequestRequest
    {
        // Stored in decisionNotes; required (the database refuses a rejected row without it)
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
