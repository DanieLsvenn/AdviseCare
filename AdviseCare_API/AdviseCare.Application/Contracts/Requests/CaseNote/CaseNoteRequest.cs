using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.CaseNote
{
    public class CreateCaseNoteRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        [Required(ErrorMessage = "Note text is required.")]
        [MaxLength(8000, ErrorMessage = "Note text must be at most 8000 characters.")]
        public string NoteText { get; set; } = string.Empty;
    }

    public class UpdateCaseNoteRequest
    {
        [Required(ErrorMessage = "Note text is required.")]
        [MaxLength(8000, ErrorMessage = "Note text must be at most 8000 characters.")]
        public string NoteText { get; set; } = string.Empty;
    }

    public class SearchCaseNoteRequest
    {
        [Required(ErrorMessage = "Case id is required.")]
        public Guid CaseId { get; set; }

        public bool? IsFinalized { get; set; }
    }
}
