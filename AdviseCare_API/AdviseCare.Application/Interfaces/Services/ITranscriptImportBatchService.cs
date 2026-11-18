using AdviseCare.Application.Contracts.Requests.TranscriptImportBatch;
using AdviseCare.Application.Contracts.Responses.TranscriptImportBatch;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ITranscriptImportBatchService
    {
        Task<IEnumerable<GetAllTranscriptImportBatchResponse>> GetAll();
        Task<IEnumerable<GetAllTranscriptImportBatchWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetTranscriptImportBatchByIdResponse> GetByIdAsync(Guid TranscriptImportBatchId);
        Task<List<SearchTranscriptImportBatchResponse>> SearchAsync(SearchTranscriptImportBatchRequest request);
        Task<GetTranscriptImportBatchByIdResponse> CreateAsync(Guid CreatorId, CreateTranscriptImportBatchRequest request);
        Task<GetTranscriptImportBatchByIdResponse> AddFilesAsync(Guid TranscriptImportBatchId, AddTranscriptImportFilesRequest request);
        Task<GetTranscriptImportBatchByIdResponse> DeleteFileAsync(Guid TranscriptImportBatchId, Guid TranscriptImportFileId);
        Task<CommitTranscriptImportBatchResponse> CommitAsync(Guid AdminId, Guid TranscriptImportBatchId);
        Task<GetTranscriptImportBatchByIdResponse> CancelAsync(Guid AdminId, Guid TranscriptImportBatchId);
    }
}
