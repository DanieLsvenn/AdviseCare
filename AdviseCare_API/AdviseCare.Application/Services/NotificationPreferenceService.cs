using AdviseCare.Application.Contracts.Requests.NotificationPreference;
using AdviseCare.Application.Contracts.Responses.NotificationPreference;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly IGenericRepository<Notificationpreference> _notificationPreferenceRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IActivityLogService _activityLogService;

        public NotificationPreferenceService(
            IGenericRepository<Notificationpreference> notificationPreferenceRepository,
            IGenericRepository<Account> accountRepository,
            IActivityLogService activityLogService)
        {
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _accountRepository = accountRepository;
            _activityLogService = activityLogService;
        }

        // Get all Notification preferences
        public async Task<IEnumerable<GetAllNotificationPreferenceResponse>> GetAll()
        {
            var notificationPreferences = await _notificationPreferenceRepository.GetAllAsyncOrderBy(p => p.Channel);

            return notificationPreferences.Select(p => new GetAllNotificationPreferenceResponse
            {
                NotificationPreferenceId = p.NotificationPreferenceId,
                AccountId = p.AccountId,
                Channel = p.Channel,
                IsEnabled = p.IsEnabled
            });
        }

        // Get all Notification preferences with pagination
        public async Task<IEnumerable<GetAllNotificationPreferenceWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var notificationPreferences = await _notificationPreferenceRepository.GetAllAsyncOrderBy(p => p.Channel);

            return notificationPreferences
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetAllNotificationPreferenceWithPaginationResponse
                {
                    NotificationPreferenceId = p.NotificationPreferenceId,
                    AccountId = p.AccountId,
                    Channel = p.Channel,
                    IsEnabled = p.IsEnabled
                });
        }

        // Get Notification preference by Id
        public async Task<GetNotificationPreferenceByIdResponse> GetByIdAsync(Guid NotificationPreferenceId)
        {
            var notificationPreference = await _notificationPreferenceRepository.GetByIdAsync(NotificationPreferenceId)
                ?? throw new NotFoundException("Notification preference not found.");

            return new GetNotificationPreferenceByIdResponse
            {
                NotificationPreferenceId = notificationPreference.NotificationPreferenceId,
                AccountId = notificationPreference.AccountId,
                Channel = notificationPreference.Channel,
                IsEnabled = notificationPreference.IsEnabled
            };
        }

        // Search Notification preferences by account, channel or state
        public async Task<List<SearchNotificationPreferenceResponse>> SearchAsync(SearchNotificationPreferenceRequest request)
        {
            var notificationPreferences = await _notificationPreferenceRepository.SearchAsyncOrderBy(p =>
                (request.AccountId == null || p.AccountId == request.AccountId) &&
                (string.IsNullOrWhiteSpace(request.Channel) || p.Channel == request.Channel) &&
                (request.IsEnabled == null || p.IsEnabled == request.IsEnabled),
                p => p.Channel);

            return notificationPreferences.Select(p => new SearchNotificationPreferenceResponse
            {
                NotificationPreferenceId = p.NotificationPreferenceId,
                AccountId = p.AccountId,
                Channel = p.Channel,
                IsEnabled = p.IsEnabled
            }).ToList();
        }

        // Create Notification preference
        public async Task<CreateNotificationPreferenceResponse> CreateAsync(CreateNotificationPreferenceRequest request)
        {
            if (!await _accountRepository.ExistsAsync(a => a.AccountId == request.AccountId))
                throw new NotFoundException("Account not found.");

            if (await _notificationPreferenceRepository.ExistsAsync(p => p.AccountId == request.AccountId && p.Channel == request.Channel))
                throw new ConflictException($"This account already has a '{request.Channel}' preference.");

            var notificationPreference = new Notificationpreference
            {
                NotificationPreferenceId = Guid.NewGuid(),
                AccountId = request.AccountId,
                Channel = request.Channel.Trim(),
                IsEnabled = request.IsEnabled
            };
            await _notificationPreferenceRepository.CreateAsync(notificationPreference);

            return new CreateNotificationPreferenceResponse
            {
                NotificationPreferenceId = notificationPreference.NotificationPreferenceId,
                AccountId = notificationPreference.AccountId,
                Channel = notificationPreference.Channel,
                IsEnabled = notificationPreference.IsEnabled
            };
        }

        // Update Notification preference
        public async Task<UpdateNotificationPreferenceResponse> UpdateAsync(Guid NotificationPreferenceId, UpdateNotificationPreferenceRequest request)
        {
            var notificationPreference = await _notificationPreferenceRepository.GetByIdAsync(NotificationPreferenceId)
                ?? throw new NotFoundException("Notification preference not found.");

            notificationPreference.IsEnabled = request.IsEnabled;
            await _notificationPreferenceRepository.UpdateAsync(notificationPreference);

            return new UpdateNotificationPreferenceResponse
            {
                NotificationPreferenceId = notificationPreference.NotificationPreferenceId,
                AccountId = notificationPreference.AccountId,
                Channel = notificationPreference.Channel,
                IsEnabled = notificationPreference.IsEnabled
            };
        }

        // Get both notification channels of an Account (email, inApp); a channel with no row is on
        public async Task<List<GetMyNotificationPreferenceResponse>> GetMyNotificationPreferencesAsync(Guid AccountId)
        {
            var preferences = await _notificationPreferenceRepository.SearchAsync(p => p.AccountId == AccountId);

            return new[] { "email", "inApp" }.Select(channel => new GetMyNotificationPreferenceResponse
            {
                Channel = channel,
                IsEnabled = preferences.FirstOrDefault(p => p.Channel == channel)?.IsEnabled ?? true
            }).ToList();
        }

        // Turn one notification channel of an Account on or off (creates the preference row when it does not exist yet)
        public async Task<UpdateMyNotificationPreferenceResponse> UpdateMyNotificationPreferenceAsync(Guid AccountId, UpdateMyNotificationPreferenceRequest request)
        {
            var preference = await _notificationPreferenceRepository.FirstOrDefaultAsync(p => p.AccountId == AccountId && p.Channel == request.Channel);
            if (preference == null)
            {
                preference = new Notificationpreference
                {
                    NotificationPreferenceId = Guid.NewGuid(),
                    AccountId = AccountId,
                    Channel = request.Channel,
                    IsEnabled = request.IsEnabled
                };
                await _notificationPreferenceRepository.CreateAsync(preference);
            }
            else
            {
                preference.IsEnabled = request.IsEnabled;
                await _notificationPreferenceRepository.UpdateAsync(preference);
            }

            await _activityLogService.LogAsync(AccountId, "notificationPreferencesUpdated", $"Turned {(request.IsEnabled ? "on" : "off")} {request.Channel} notifications");

            return new UpdateMyNotificationPreferenceResponse
            {
                Channel = preference.Channel,
                IsEnabled = preference.IsEnabled
            };
        }
    }
}
