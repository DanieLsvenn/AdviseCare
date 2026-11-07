namespace AdviseCare.Application.Contracts.Responses.CaseMessage
{
    public class GetAllCaseMessageResponse
    {
        public Guid CaseMessageId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class GetAllCaseMessageWithPaginationResponse
    {
        public Guid CaseMessageId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class GetCaseMessageByIdResponse
    {
        public Guid CaseMessageId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SearchCaseMessageResponse
    {
        public Guid CaseMessageId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCaseMessageResponse
    {
        public Guid CaseMessageId { get; set; } = Guid.Empty;
        public Guid AuthorId { get; set; } = Guid.Empty;
        public Guid CaseId { get; set; } = Guid.Empty;
        public string AuthorFullName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
