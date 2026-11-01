namespace AdviseCare.Application.Contracts.Responses.CaseNote
{
    public class CaseNoteResponse
    {
        public Guid CaseNoteId { get; set; }
        public Guid CaseId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsFinalized { get; set; }
    }
}
