using AdviseCare.Application.Contracts.Requests.AcademicResult;
using AdviseCare.Application.Contracts.Responses.AcademicResult;

namespace AdviseCare.Application.Interfaces.Services
{
    // Academic results (STUDENTSUBJECTRESULTS): per-subject grade and attendance per term, imported from transcript PDFs
    // in batches (TRANSCRIPTIMPORTBATCHES / TRANSCRIPTIMPORTFILES), read by the student and, with consent, by staff
    public interface IAcademicResultService
    {
        Task<TranscriptImportBatchResponse> CreateBatchAsync(Guid AdminId, UploadTranscriptsRequest request);
        Task<TranscriptImportBatchResponse> AddFilesAsync(Guid AdminId, Guid ImportBatchId, UploadTranscriptsRequest request);
        Task<TranscriptImportBatchResponse> GetBatchAsync(Guid ImportBatchId);
        Task<List<TranscriptImportBatchSummaryResponse>> GetBatchesAsync(int Count);
        Task<CommitTranscriptImportResponse> CommitAsync(Guid AdminId, Guid ImportBatchId, CommitTranscriptImportRequest request);
        Task<TranscriptImportBatchResponse> CancelAsync(Guid AdminId, Guid ImportBatchId);
        Task<AcademicResultsResponse> GetMyResultsAsync(Guid StudentId, Guid? TermId);
        Task<AcademicResultsResponse> GetStudentResultsAsync(Guid AccessorId, Guid StudentId, GetAcademicResultsRequest request);
    }
}
