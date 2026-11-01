using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AssignmentChangeRequest
{
    public class CreateAssignmentChangeRequestRequest
    {
        [Required(ErrorMessage = "Advisor assignment id is required.")]
        public Guid AdvisorAssignmentId { get; set; }

        [Required(ErrorMessage = "Proposed advisor id is required.")]
        public Guid ProposedAdvisorId { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(4000, ErrorMessage = "Reason must be at most 4000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class SearchAssignmentChangeRequestRequest
    {
        public Guid? AdvisorAssignmentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RejectAssignmentChangeRequestRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
