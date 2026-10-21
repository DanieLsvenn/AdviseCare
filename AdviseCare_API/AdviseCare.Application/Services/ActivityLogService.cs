using AdviseCare.Application.Contracts.Requests.ActivityLog;
using AdviseCare.Application.Contracts.Responses.ActivityLog;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AdviseCare.Application.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IGenericRepository<Activitylog> _activityLogRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            IGenericRepository<Activitylog> activityLogRepository,
            IGenericRepository<Account> accountRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _activityLogRepository = activityLogRepository;
            _accountRepository = accountRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        // Get all Activity logs
        public async Task<IEnumerable<GetAllActivityLogResponse>> GetAll()
        {
            var activityLogs = await _activityLogRepository.GetAllAsyncOrderBy(a => a.OccurredAt, false);

            return activityLogs.Select(a => new GetAllActivityLogResponse
            {
                ActivityLogId = a.ActivityLogId,
                AccountId = a.AccountId,
                ActivityDetail = a.ActivityDetail ?? string.Empty,
                ActivityType = a.ActivityType,
                OccurredAt = a.OccurredAt
            });
        }

        // Get all Activity logs with pagination
        public async Task<IEnumerable<GetAllActivityLogWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var activityLogs = await _activityLogRepository.GetAllAsyncOrderBy(a => a.OccurredAt, false);

            return activityLogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new GetAllActivityLogWithPaginationResponse
                {
                    ActivityLogId = a.ActivityLogId,
                    AccountId = a.AccountId,
                    ActivityDetail = a.ActivityDetail ?? string.Empty,
                    ActivityType = a.ActivityType,
                    OccurredAt = a.OccurredAt
                });
        }

        // Get Activity log by Id
        public async Task<GetActivityLogByIdResponse> GetByIdAsync(Guid ActivityLogId)
        {
            var activityLog = await _activityLogRepository.GetByIdAsync(ActivityLogId)
                ?? throw new NotFoundException("Activity log not found.");

            return new GetActivityLogByIdResponse
            {
                ActivityLogId = activityLog.ActivityLogId,
                AccountId = activityLog.AccountId,
                ActivityDetail = activityLog.ActivityDetail ?? string.Empty,
                ActivityType = activityLog.ActivityType,
                OccurredAt = activityLog.OccurredAt
            };
        }

        // Search Activity logs by account or activity type
        public async Task<List<SearchActivityLogResponse>> SearchAsync(SearchActivityLogRequest request)
        {
            var activityLogs = await _activityLogRepository.SearchAsyncOrderBy(a =>
                (request.AccountId == null || a.AccountId == request.AccountId) &&
                (string.IsNullOrWhiteSpace(request.ActivityType) || a.ActivityType == request.ActivityType),
                a => a.OccurredAt, false);

            return activityLogs.Select(a => new SearchActivityLogResponse
            {
                ActivityLogId = a.ActivityLogId,
                AccountId = a.AccountId,
                ActivityDetail = a.ActivityDetail ?? string.Empty,
                ActivityType = a.ActivityType,
                OccurredAt = a.OccurredAt
            }).ToList();
        }

        // Create Activity log
        public async Task<CreateActivityLogResponse> CreateAsync(CreateActivityLogRequest request)
        {
            if (!await _accountRepository.ExistsAsync(a => a.AccountId == request.AccountId))
                throw new NotFoundException("Account not found.");

            var activityLog = new Activitylog
            {
                ActivityLogId = Guid.NewGuid(),
                AccountId = request.AccountId,
                ActivityDetail = string.IsNullOrWhiteSpace(request.ActivityDetail) ? null : request.ActivityDetail.Trim(),
                ActivityType = request.ActivityType.Trim(),
                OccurredAt = DateTime.UtcNow
            };
            await _activityLogRepository.CreateAsync(activityLog);

            return new CreateActivityLogResponse
            {
                ActivityLogId = activityLog.ActivityLogId,
                AccountId = activityLog.AccountId,
                ActivityDetail = activityLog.ActivityDetail ?? string.Empty,
                ActivityType = activityLog.ActivityType,
                OccurredAt = activityLog.OccurredAt
            };
        }

        // Update Activity log
        public async Task<UpdateActivityLogResponse> UpdateAsync(Guid ActivityLogId, UpdateActivityLogRequest request)
        {
            var activityLog = await _activityLogRepository.GetByIdAsync(ActivityLogId)
                ?? throw new NotFoundException("Activity log not found.");

            activityLog.ActivityDetail = string.IsNullOrWhiteSpace(request.ActivityDetail) ? null : request.ActivityDetail.Trim();
            activityLog.ActivityType = request.ActivityType.Trim();
            await _activityLogRepository.UpdateAsync(activityLog);

            return new UpdateActivityLogResponse
            {
                ActivityLogId = activityLog.ActivityLogId,
                AccountId = activityLog.AccountId,
                ActivityDetail = activityLog.ActivityDetail ?? string.Empty,
                ActivityType = activityLog.ActivityType,
                OccurredAt = activityLog.OccurredAt
            };
        }

        // Get the activity history of an Account, newest first
        public async Task<List<GetMyActivityLogResponse>> GetMyActivityLogsAsync(Guid AccountId)
        {
            var activityLogs = await _activityLogRepository.SearchAsyncOrderBy(a => a.AccountId == AccountId, a => a.OccurredAt, false);

            return activityLogs.Select(a => new GetMyActivityLogResponse
            {
                ActivityLogId = a.ActivityLogId,
                ActivityDetail = a.ActivityDetail ?? string.Empty,
                ActivityType = a.ActivityType,
                OccurredAt = a.OccurredAt
            }).ToList();
        }

        // Log an activity for the given Account (types are listed in AdviseCare_Activity_Types.txt)
        public async Task LogAsync(Guid AccountId, string activityType, string activityDetail)
        {
            await _activityLogRepository.CreateAsync(new Activitylog
            {
                ActivityLogId = Guid.NewGuid(),
                AccountId = AccountId,
                ActivityDetail = activityDetail,
                ActivityType = activityType,
                OccurredAt = DateTime.UtcNow
            });
        }

        // Log an activity for the signed-in Account of the current request (skipped when nobody is signed in)
        public async Task LogCurrentAccountAsync(string activityType, string activityDetail)
        {
            var accountIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Sid);
            if (Guid.TryParse(accountIdClaim, out var accountId))
                await LogAsync(accountId, activityType, activityDetail);
        }
    }
}
