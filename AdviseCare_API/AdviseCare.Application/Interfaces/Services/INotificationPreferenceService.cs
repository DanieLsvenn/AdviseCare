using AdviseCare.Application.Contracts.Requests.NotificationPreference;
using AdviseCare.Application.Contracts.Responses.NotificationPreference;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface INotificationPreferenceService
    {
        Task<IEnumerable<GetAllNotificationPreferenceResponse>> GetAll();
        Task<IEnumerable<GetAllNotificationPreferenceWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetNotificationPreferenceByIdResponse> GetByIdAsync(Guid NotificationPreferenceId);
        Task<List<SearchNotificationPreferenceResponse>> SearchAsync(SearchNotificationPreferenceRequest request);
        Task<CreateNotificationPreferenceResponse> CreateAsync(CreateNotificationPreferenceRequest request);
        Task<UpdateNotificationPreferenceResponse> UpdateAsync(Guid NotificationPreferenceId, UpdateNotificationPreferenceRequest request);
        Task<List<GetMyNotificationPreferenceResponse>> GetMyNotificationPreferencesAsync(Guid AccountId);
        Task<UpdateMyNotificationPreferenceResponse> UpdateMyNotificationPreferenceAsync(Guid AccountId, UpdateMyNotificationPreferenceRequest request);
    }
}
