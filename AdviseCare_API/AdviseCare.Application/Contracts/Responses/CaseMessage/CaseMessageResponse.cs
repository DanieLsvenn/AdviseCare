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
        public List<CaseMessageAttachmentResponse> Attachments { get; set; } = new();
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
        public List<CaseMessageAttachmentResponse> Attachments { get; set; } = new();
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
        public List<CaseMessageAttachmentResponse> Attachments { get; set; } = new();
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
        public List<CaseMessageAttachmentResponse> Attachments { get; set; } = new();
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
        public List<CaseMessageAttachmentResponse> Attachments { get; set; } = new();
    }

    public class CaseMessageAttachmentResponse
    {
        public Guid CaseMessageAttachmentId { get; set; } = Guid.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int FileSize { get; set; } = 0;
    }

    public class DownloadCaseMessageAttachmentResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
