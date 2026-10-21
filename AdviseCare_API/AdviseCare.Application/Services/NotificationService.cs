using AdviseCare.Application.Contracts.Requests.Notification;
using AdviseCare.Application.Contracts.Responses.Notification;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Email;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AdviseCare.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IGenericRepository<Notification> _notificationRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<Notificationpreference> _notificationPreferenceRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IGenericRepository<Notification> notificationRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<Notificationpreference> notificationPreferenceRepository,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _accountRepository = accountRepository;
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _emailService = emailService;
            _logger = logger;
        }

        // Get all Notifications
        public async Task<IEnumerable<GetAllNotificationResponse>> GetAll()
        {
            var notifications = await _notificationRepository.GetAllAsyncOrderBy(n => n.CreatedAt, false);

            return notifications.Select(n => new GetAllNotificationResponse
            {
                NotificationId = n.NotificationId,
                AccountId = n.AccountId,
                Message = n.Message,
                RedirectUrl = n.RedirectUrl ?? string.Empty,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            });
        }

        // Get all Notifications with pagination
        public async Task<IEnumerable<GetAllNotificationWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var notifications = await _notificationRepository.GetAllAsyncOrderBy(n => n.CreatedAt, false);

            return notifications
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new GetAllNotificationWithPaginationResponse
                {
                    NotificationId = n.NotificationId,
                    AccountId = n.AccountId,
                    Message = n.Message,
                    RedirectUrl = n.RedirectUrl ?? string.Empty,
                    Title = n.Title,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                });
        }

        // Get Notification by Id
        public async Task<GetNotificationByIdResponse> GetByIdAsync(Guid NotificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(NotificationId)
                ?? throw new NotFoundException("Notification not found.");

            return new GetNotificationByIdResponse
            {
                NotificationId = notification.NotificationId,
                AccountId = notification.AccountId,
                Message = notification.Message,
                RedirectUrl = notification.RedirectUrl ?? string.Empty,
                Title = notification.Title,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            };
        }

        // Search Notifications by account, title or read state
        public async Task<List<SearchNotificationResponse>> SearchAsync(SearchNotificationRequest request)
        {
            var notifications = await _notificationRepository.SearchAsyncOrderBy(n =>
                (request.AccountId == null || n.AccountId == request.AccountId) &&
                (string.IsNullOrWhiteSpace(request.Title) || n.Title.Contains(request.Title)) &&
                (request.IsRead == null || n.IsRead == request.IsRead),
                n => n.CreatedAt, false);

            return notifications.Select(n => new SearchNotificationResponse
            {
                NotificationId = n.NotificationId,
                AccountId = n.AccountId,
                Message = n.Message,
                RedirectUrl = n.RedirectUrl ?? string.Empty,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            }).ToList();
        }

        // Create Notification
        public async Task<CreateNotificationResponse> CreateAsync(CreateNotificationRequest request)
        {
            if (!await _accountRepository.ExistsAsync(a => a.AccountId == request.AccountId))
                throw new NotFoundException("Account not found.");

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                AccountId = request.AccountId,
                Message = request.Message.Trim(),
                RedirectUrl = string.IsNullOrWhiteSpace(request.RedirectUrl) ? null : request.RedirectUrl.Trim(),
                Title = request.Title.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationRepository.CreateAsync(notification);

            return new CreateNotificationResponse
            {
                NotificationId = notification.NotificationId,
                AccountId = notification.AccountId,
                Message = notification.Message,
                RedirectUrl = notification.RedirectUrl ?? string.Empty,
                Title = notification.Title,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            };
        }

        // Update Notification
        public async Task<UpdateNotificationResponse> UpdateAsync(Guid NotificationId, UpdateNotificationRequest request)
        {
            var notification = await _notificationRepository.GetByIdAsync(NotificationId)
                ?? throw new NotFoundException("Notification not found.");

            notification.Message = request.Message.Trim();
            notification.RedirectUrl = string.IsNullOrWhiteSpace(request.RedirectUrl) ? null : request.RedirectUrl.Trim();
            notification.Title = request.Title.Trim();
            notification.IsRead = request.IsRead;
            await _notificationRepository.UpdateAsync(notification);

            return new UpdateNotificationResponse
            {
                NotificationId = notification.NotificationId,
                AccountId = notification.AccountId,
                Message = notification.Message,
                RedirectUrl = notification.RedirectUrl ?? string.Empty,
                Title = notification.Title,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            };
        }

        // Delete Notification
        public async Task<bool> DeleteAsync(Guid NotificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(NotificationId)
                ?? throw new NotFoundException("Notification not found.");

            await _notificationRepository.RemoveAsync(notification);
            return true;
        }

        // Get the notifications of an Account, newest first
        public async Task<List<GetMyNotificationResponse>> GetMyNotificationsAsync(Guid AccountId)
        {
            var notifications = await _notificationRepository.SearchAsyncOrderBy(n => n.AccountId == AccountId, n => n.CreatedAt, false);

            return notifications.Select(n => new GetMyNotificationResponse
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                RedirectUrl = n.RedirectUrl ?? string.Empty,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            }).ToList();
        }

        // Count the unread notifications of an Account, for the unread badge
        public async Task<int> GetMyUnreadCountAsync(Guid AccountId)
        {
            return await _notificationRepository.CountAsync(n => n.AccountId == AccountId && !n.IsRead);
        }

        // Mark one notification of an Account as read
        public async Task<bool> MarkAsReadAsync(Guid AccountId, Guid NotificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(NotificationId);
            if (notification == null || notification.AccountId != AccountId)
                throw new NotFoundException("Notification not found.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }
            return true;
        }

        // Mark every unread notification of an Account as read
        public async Task<int> MarkAllAsReadAsync(Guid AccountId)
        {
            var unreadNotifications = await _notificationRepository.SearchAsync(n => n.AccountId == AccountId && !n.IsRead);
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                _notificationRepository.PrepareUpdate(notification);
            }
            await _notificationRepository.SaveAsync();

            return unreadNotifications.Count;
        }

        // Delete one notification of an Account
        public async Task<bool> DeleteMyNotificationAsync(Guid AccountId, Guid NotificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(NotificationId);
            if (notification == null || notification.AccountId != AccountId)
                throw new NotFoundException("Notification not found.");

            await _notificationRepository.RemoveAsync(notification);
            return true;
        }

        // Clear (delete) every read notification of an Account; unread ones are kept
        public async Task<int> ClearMyReadNotificationsAsync(Guid AccountId)
        {
            var readNotifications = await _notificationRepository.SearchAsync(n => n.AccountId == AccountId && n.IsRead);
            foreach (var notification in readNotifications)
                _notificationRepository.PrepareRemove(notification);
            await _notificationRepository.SaveAsync();

            return readNotifications.Count;
        }

        // Notify an Account: writes the in-app notification and also emails it when the Account's email channel is on
        // (a channel with no preference row counts as on). Used by the other features, e.g. "case assigned" or "referral received".
        public async Task NotifyAsync(Guid AccountId, string title, string message, string? redirectUrl)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            if (await IsChannelEnabledAsync(AccountId, "inApp"))
            {
                await _notificationRepository.CreateAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    AccountId = AccountId,
                    Message = message,
                    RedirectUrl = redirectUrl,
                    Title = title,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            if (await IsChannelEnabledAsync(AccountId, "email"))
            {
                // A failed email must not undo the action that triggered the notification, so it is only logged
                try
                {
                    await _emailService.SendNotificationEmailAsync(account.Email, account.FullName, title, message, redirectUrl);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Notification email to {Email} could not be sent.", account.Email);
                }
            }
        }

        // Helper: whether a notification channel (email/inApp) is on for an Account; no preference row means on
        private async Task<bool> IsChannelEnabledAsync(Guid accountId, string channel)
        {
            var preference = await _notificationPreferenceRepository.FirstOrDefaultAsync(p => p.AccountId == accountId && p.Channel == channel);
            return preference == null || preference.IsEnabled;
        }
    }
}
