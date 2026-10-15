using AdviseCare.Application.Contracts.Requests.Auth;
using AdviseCare.Application.Contracts.Responses.Auth;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Session> _sessionRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Session> sessionRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _sessionRepository = sessionRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        // Log an Account in with email/username and password, opening a Session and issuing a JWT
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var identifier = request.Identifier.Trim();

            // Same message for "no such account", "no password set yet" and "wrong password",
            // so the endpoint can't be used to find out which emails/usernames exist.
            var account = await _accountRepository.FirstOrDefaultAsync(a => a.Email == identifier || a.UserName == identifier)
                ?? throw new UnauthorizedException("Invalid email/username or password.");

            if (string.IsNullOrEmpty(account.PasswordHash) || !_passwordHasher.Verify(request.Password, account.PasswordHash))
                throw new UnauthorizedException("Invalid email/username or password.");

            if (account.Status != "active")
                throw new ForbiddenException($"Account is not active. Current status: {account.Status}.");

            // Open the Session and record the sign-in method in one save
            var session = new Session
            {
                AccountId = account.AccountId,
                DeviceInfo = string.IsNullOrWhiteSpace(request.DeviceInfo) ? null : request.DeviceInfo,
                LoginAt = DateTime.UtcNow,
                IsActive = true
            };
            _sessionRepository.PrepareCreate(session);
            account.AuthProvider = "password";
            await _sessionRepository.SaveAsync();

            var roles = await GetRoleNamesAsync(account.AccountId);
            var token = _jwtTokenGenerator.GenerateToken(account, session.SessionId, roles);

            return new LoginResponse
            {
                AccountId = account.AccountId,
                SessionId = session.SessionId,
                UserName = account.UserName,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = roles,
                Token = token
            };
        }

        // Get the profile of the currently authenticated Account
        public async Task<GetSelfProfileResponse> GetSelfProfileAsync(Guid accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId)
                ?? throw new NotFoundException("Account not found.");

            return new GetSelfProfileResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                UserName = account.UserName,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                AuthProvider = account.AuthProvider ?? string.Empty,
                CreatedAt = account.CreatedAt,
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = await GetRoleNamesAsync(account.AccountId)
            };
        }

        // Log the current Session out, so its JWT stops working
        public async Task<LogoutResponse> LogoutAsync(Guid accountId, Guid sessionId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.AccountId != accountId)
                throw new NotFoundException("Session not found.");

            if (session.IsActive)
            {
                session.IsActive = false;
                session.LogoutAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);
            }

            return new LogoutResponse { Message = "Logged out successfully." };
        }

        // Get the role names of an Account (exact ROLES.roleName values, used as JWT role claims)
        private async Task<List<string>> GetRoleNamesAsync(Guid accountId)
        {
            var accountRoles = await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == accountId, ar => ar.Role);
            return accountRoles.Select(ar => ar.Role.RoleName).ToList();
        }
    }
}
