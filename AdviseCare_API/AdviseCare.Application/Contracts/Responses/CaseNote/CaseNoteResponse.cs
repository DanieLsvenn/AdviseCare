namespace AdviseCare.Application.Contracts.Responses.CaseNote
{
    public class GetAllCaseNoteResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class GetAllCaseNoteWithPaginationResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class GetCaseNoteByIdResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class SearchCaseNoteResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class CreateCaseNoteResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class UpdateCaseNoteResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }

    public class FinalizeCaseNoteResponse
    {
        public Guid CaseNoteId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; } = false;
    }
}
