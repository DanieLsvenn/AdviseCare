using AdviseCare.Application.Contracts.Requests.AccessLog;
using AdviseCare.Application.Contracts.Responses.AccessLog;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAccessLogService
    {
        Task<IEnumerable<GetAllAccessLogResponse>> GetAll();
        Task<IEnumerable<GetAllAccessLogWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetAccessLogByIdResponse> GetByIdAsync(Guid AccessLogId);
        Task<List<SearchAccessLogResponse>> SearchAsync(SearchAccessLogRequest request);
        Task<bool> ReviewAsync(Guid AccessLogId, ReviewAccessLogRequest request);
        Task<bool> FlagAsync(Guid AccessLogId);
        Task<List<GetMyAccessLogResponse>> GetMyAccessLogsAsync(Guid StudentId, SearchMyAccessLogRequest request);
        Task<bool> FlagMineAsync(Guid StudentId, Guid AccessLogId);
        Task<ExportProcessingRecordResponse> ExportProcessingRecordAsync(ExportProcessingRecordRequest request);
    }
}
