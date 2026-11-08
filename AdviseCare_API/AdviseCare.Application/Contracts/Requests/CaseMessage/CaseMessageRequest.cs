using Microsoft.AspNetCore.Http;
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

    // multipart/form-data: a message with up to 5 files (10 MB each)
    public class CreateCaseMessageWithFilesRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Message text is required."), StringLength(1000,
            ErrorMessage = "Message text cannot exceed 1000 characters.")]
        public string MessageText { get; set; } = string.Empty;
        public List<IFormFile> Files { get; set; } = new();
    }
}
