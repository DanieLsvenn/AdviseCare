using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.AdvisorAssignment
{
    public class SearchAdvisorAssignmentRequest
    {
        public Guid? AdvisorId { get; set; }
        public Guid? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReassignAdvisorAssignmentRequest
    {
        [Required(ErrorMessage = "Advisor id is required.")]
        public Guid AdvisorId { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason must be at most 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
