namespace AdviseCare.Application.Contracts.Responses.TranscriptImportBatch
{
    public class GetAllTranscriptImportBatchResponse
    {
        public Guid TranscriptImportBatchId { get; set; } = Guid.Empty;
        public Guid ImportedBy { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        public int TotalFiles { get; set; } = 0;
        public int ValidFiles { get; set; } = 0;
        public int InvalidFiles { get; set; } = 0;
        public int CommittedFiles { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
    }

    public class GetAllTranscriptImportBatchWithPaginationResponse
    {
        public Guid TranscriptImportBatchId { get; set; } = Guid.Empty;
        public Guid ImportedBy { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        public int TotalFiles { get; set; } = 0;
        public int ValidFiles { get; set; } = 0;
        public int InvalidFiles { get; set; } = 0;
        public int CommittedFiles { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
    }

    // The batch with every file's check result: the preview while previewed, the record once committed or cancelled
    public class GetTranscriptImportBatchByIdResponse
    {
        public Guid TranscriptImportBatchId { get; set; } = Guid.Empty;
        public Guid ImportedBy { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        public int TotalFiles { get; set; } = 0;
        public int ValidFiles { get; set; } = 0;
        public int InvalidFiles { get; set; } = 0;
        public int CommittedFiles { get; set; } = 0;
        // Values the commit will add (InsertCount) and how many of them replace a current value (ReplaceCount), over the valid files
        public int InsertCount { get; set; } = 0;
        public int ReplaceCount { get; set; } = 0;
        public List<TranscriptImportFileResponse> Files { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class SearchTranscriptImportBatchResponse
    {
        public Guid TranscriptImportBatchId { get; set; } = Guid.Empty;
        public Guid ImportedBy { get; set; } = Guid.Empty;
        public string ImportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CommittedAt { get; set; }
        public int TotalFiles { get; set; } = 0;
        public int ValidFiles { get; set; } = 0;
        public int InvalidFiles { get; set; } = 0;
        public int CommittedFiles { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
    }

    public class CommitTranscriptImportBatchResponse
    {
        public Guid TranscriptImportBatchId { get; set; } = Guid.Empty;
        public int CommittedFiles { get; set; } = 0;
        // STUDENTDATA rows written, and how many of them replace a value the student already had (insert-only ledger: the old row stays)
        public int InsertedCount { get; set; } = 0;
        public int ReplacedCount { get; set; } = 0;
    }

    // One PDF of a batch
    public class TranscriptImportFileResponse
    {
        public Guid TranscriptImportFileId { get; set; } = Guid.Empty;
        public Guid? StudentId { get; set; }
        public string CurriculumCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FullNameInFile { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int RowCount { get; set; } = 0;
        // Values the commit will add / replace, and rows left out (no semester yet, or no value such as an exempt subject)
        public int InsertCount { get; set; } = 0;
        public int ReplaceCount { get; set; } = 0;
        public int SkippedRowCount { get; set; } = 0;
        public DateTime UploadedAt { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        // grade / attendance; empty when the file could not be read
        public string ImportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}