using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseNote
{
    public class SearchCaseNoteRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        public bool? IsFinalized { get; set; }
    }

    public class CreateCaseNoteRequest
    {
        [Required(ErrorMessage = "Case is required.")]
        public Guid CaseId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Note text is required.")]
        public string NoteText { get; set; } = string.Empty;
    }

    public class UpdateCaseNoteRequest
    {
        [Required(ErrorMessage = "Note text is required.")]
        public string NoteText { get; set; } = string.Empty;
    }
}
