using AdviseCare.Application.Contracts.Requests.Session;
using AdviseCare.Application.Contracts.Responses.Session;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class SessionService : ISessionService
    {
        // A session (and its JWT) lasts 1 month after sign-in
        public const int LifetimeDays = 30;

        private readonly IGenericRepository<Session> _sessionRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IActivityLogService _activityLogService;

        public SessionService(
            IGenericRepository<Session> sessionRepository,
            IGenericRepository<Account> accountRepository,
            IActivityLogService activityLogService)
        {
            _sessionRepository = sessionRepository;
            _accountRepository = accountRepository;
            _activityLogService = activityLogService;
        }

        // Get all Sessions
        public async Task<IEnumerable<GetAllSessionResponse>> GetAll()
        {
            var sessions = await _sessionRepository.GetAllAsyncOrderBy(s => s.LoginAt, false);

            return sessions.Select(s => new GetAllSessionResponse
            {
                SessionId = s.SessionId,
                AccountId = s.AccountId,
                DeviceInfo = s.DeviceInfo ?? string.Empty,
                LoginAt = s.LoginAt,
                LogoutAt = s.LogoutAt,
                IsActive = s.IsActive
            });
        }

        // Get all Sessions with pagination
        public async Task<IEnumerable<GetAllSessionWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var sessions = await _sessionRepository.GetAllAsyncOrderBy(s => s.LoginAt, false);

            return sessions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new GetAllSessionWithPaginationResponse
                {
                    SessionId = s.SessionId,
                    AccountId = s.AccountId,
                    DeviceInfo = s.DeviceInfo ?? string.Empty,
                    LoginAt = s.LoginAt,
                    LogoutAt = s.LogoutAt,
                    IsActive = s.IsActive
                });
        }

        // Get Session by Id
        public async Task<GetSessionByIdResponse> GetByIdAsync(Guid SessionId)
        {
            var session = await _sessionRepository.GetByIdAsync(SessionId)
                ?? throw new NotFoundException("Session not found.");

            return new GetSessionByIdResponse
            {
                SessionId = session.SessionId,
                AccountId = session.AccountId,
                DeviceInfo = session.DeviceInfo ?? string.Empty,
                LoginAt = session.LoginAt,
                LogoutAt = session.LogoutAt,
                IsActive = session.IsActive
            };
        }

        // Search Sessions by account or active state
        public async Task<List<SearchSessionResponse>> SearchAsync(SearchSessionRequest request)
        {
            var sessions = await _sessionRepository.SearchAsyncOrderBy(s =>
                (request.AccountId == null || s.AccountId == request.AccountId) &&
                (request.IsActive == null || s.IsActive == request.IsActive),
                s => s.LoginAt, false);

            return sessions.Select(s => new SearchSessionResponse
            {
                SessionId = s.SessionId,
                AccountId = s.AccountId,
                DeviceInfo = s.DeviceInfo ?? string.Empty,
                LoginAt = s.LoginAt,
                LogoutAt = s.LogoutAt,
                IsActive = s.IsActive
            }).ToList();
        }

        // Create Session
        public async Task<CreateSessionResponse> CreateAsync(CreateSessionRequest request)
        {
            if (!await _accountRepository.ExistsAsync(a => a.AccountId == request.AccountId))
                throw new NotFoundException("Account not found.");

            var session = new Session
            {
                SessionId = Guid.NewGuid(),
                AccountId = request.AccountId,
                DeviceInfo = string.IsNullOrWhiteSpace(request.DeviceInfo) ? null : request.DeviceInfo.Trim(),
                LoginAt = DateTime.UtcNow,
                IsActive = true
            };
            await _sessionRepository.CreateAsync(session);

            return new CreateSessionResponse
            {
                SessionId = session.SessionId,
                AccountId = session.AccountId,
                DeviceInfo = session.DeviceInfo ?? string.Empty,
                LoginAt = session.LoginAt,
                LogoutAt = session.LogoutAt,
                IsActive = session.IsActive
            };
        }

        // Update Session
        public async Task<UpdateSessionResponse> UpdateAsync(Guid SessionId, UpdateSessionRequest request)
        {
            var session = await _sessionRepository.GetByIdAsync(SessionId)
                ?? throw new NotFoundException("Session not found.");

            session.IsActive = request.IsActive;
            session.LogoutAt = session.IsActive ? null : session.LogoutAt ?? DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            return new UpdateSessionResponse
            {
                SessionId = session.SessionId,
                AccountId = session.AccountId,
                DeviceInfo = session.DeviceInfo ?? string.Empty,
                LoginAt = session.LoginAt,
                LogoutAt = session.LogoutAt,
                IsActive = session.IsActive
            };
        }

        // Get the live sessions of an Account (active and not older than the session lifetime), newest first
        public async Task<List<GetMySessionResponse>> GetMySessionsAsync(Guid AccountId, Guid CurrentSessionId)
        {
            var cutoff = DateTime.UtcNow.AddDays(-LifetimeDays);
            var sessions = await _sessionRepository.SearchAsyncOrderBy(s => s.AccountId == AccountId && s.IsActive && s.LoginAt > cutoff, s => s.LoginAt, false);

            return sessions.Select(s => new GetMySessionResponse
            {
                SessionId = s.SessionId,
                DeviceInfo = s.DeviceInfo ?? string.Empty,
                LoginAt = s.LoginAt,
                ExpiresAt = s.LoginAt.AddDays(LifetimeDays),
                IsCurrent = s.SessionId == CurrentSessionId
            }).ToList();
        }

        // Log out one session of an Account: the current one (logout) or another one chosen from the session list
        public async Task<LogoutResponse> LogoutAsync(Guid AccountId, Guid CurrentSessionId, Guid SessionId)
        {
            var session = await _sessionRepository.GetByIdAsync(SessionId);
            if (session == null || session.AccountId != AccountId)
                throw new NotFoundException("Session not found.");

            if (session.IsActive)
            {
                session.IsActive = false;
                session.LogoutAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);

                if (SessionId == CurrentSessionId)
                    await _activityLogService.LogAsync(AccountId, "signedOut", $"Signed out ({session.DeviceInfo ?? "unknown device"})");
                else
                    await _activityLogService.LogAsync(AccountId, "otherSessionsSignedOut", $"Signed out another session ({session.DeviceInfo ?? "unknown device"})");
            }

            return new LogoutResponse { Message = "Logged out successfully." };
        }

        // Log out every other active session of an Account, keeping the current one
        public async Task<LogoutResponse> LogoutOtherSessionsAsync(Guid AccountId, Guid CurrentSessionId)
        {
            var otherSessions = await _sessionRepository.SearchAsync(s => s.AccountId == AccountId && s.IsActive && s.SessionId != CurrentSessionId);
            foreach (var otherSession in otherSessions)
            {
                otherSession.IsActive = false;
                otherSession.LogoutAt = DateTime.UtcNow;
                _sessionRepository.PrepareUpdate(otherSession);
            }
            await _sessionRepository.SaveAsync();

            if (otherSessions.Count > 0)
                await _activityLogService.LogAsync(AccountId, "otherSessionsSignedOut", $"Signed out {otherSessions.Count} other session(s)");

            return new LogoutResponse { Message = $"Logged out of {otherSessions.Count} other session(s)." };
        }
    }
}
