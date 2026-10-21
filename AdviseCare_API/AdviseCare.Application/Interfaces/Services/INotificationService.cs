using AdviseCare.Application.Contracts.Requests.Notification;
using AdviseCare.Application.Contracts.Responses.Notification;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<GetAllNotificationResponse>> GetAll();
        Task<IEnumerable<GetAllNotificationWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetNotificationByIdResponse> GetByIdAsync(Guid NotificationId);
        Task<List<SearchNotificationResponse>> SearchAsync(SearchNotificationRequest request);
        Task<CreateNotificationResponse> CreateAsync(CreateNotificationRequest request);
        Task<UpdateNotificationResponse> UpdateAsync(Guid NotificationId, UpdateNotificationRequest request);
        Task<bool> DeleteAsync(Guid NotificationId);
        Task<List<GetMyNotificationResponse>> GetMyNotificationsAsync(Guid AccountId);
        Task<int> GetMyUnreadCountAsync(Guid AccountId);
        Task<bool> MarkAsReadAsync(Guid AccountId, Guid NotificationId);
        Task<int> MarkAllAsReadAsync(Guid AccountId);
        Task<bool> DeleteMyNotificationAsync(Guid AccountId, Guid NotificationId);
        Task<int> ClearMyReadNotificationsAsync(Guid AccountId);
        Task NotifyAsync(Guid AccountId, string title, string message, string? redirectUrl);
    }
}
