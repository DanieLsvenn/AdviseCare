using AdviseCare.Application.Contracts.Requests.ActivityLog;
using AdviseCare.Application.Contracts.Responses.ActivityLog;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IActivityLogService
    {
        Task<IEnumerable<GetAllActivityLogResponse>> GetAll();
        Task<IEnumerable<GetAllActivityLogWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetActivityLogByIdResponse> GetByIdAsync(Guid ActivityLogId);
        Task<List<SearchActivityLogResponse>> SearchAsync(SearchActivityLogRequest request);
        Task<CreateActivityLogResponse> CreateAsync(CreateActivityLogRequest request);
        Task<UpdateActivityLogResponse> UpdateAsync(Guid ActivityLogId, UpdateActivityLogRequest request);
        Task<List<GetMyActivityLogResponse>> GetMyActivityLogsAsync(Guid AccountId);
        Task LogAsync(Guid AccountId, string activityType, string activityDetail);
        Task LogCurrentAccountAsync(string activityType, string activityDetail);
    }
}
