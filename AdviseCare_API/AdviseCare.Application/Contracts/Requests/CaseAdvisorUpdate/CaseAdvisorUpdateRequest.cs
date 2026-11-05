using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseAdvisorUpdate
{
    public class CreateCaseAdvisorUpdateRequest
    {
        [Required(ErrorMessage = "Update text is required.")]
        [StringLength(1000, ErrorMessage = "Update text cannot exceed 1000 characters.")]
        public string MessageText { get; set; } = string.Empty;
    }
}
