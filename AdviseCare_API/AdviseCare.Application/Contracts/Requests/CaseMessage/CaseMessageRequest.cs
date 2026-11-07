using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseMessage
{
    public class SearchCaseMessageRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
    }

    public class CreateCaseMessageRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Message text is required."), StringLength(1000,
            ErrorMessage = "Message text cannot exceed 1000 characters.")]
        public string MessageText { get; set; } = string.Empty;
    }
}
