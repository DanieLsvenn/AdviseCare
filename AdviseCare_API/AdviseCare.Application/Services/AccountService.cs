using AdviseCare.Application.Contracts.Requests.Account;
using AdviseCare.Application.Contracts.Responses.Account;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Email;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Application.Interfaces.Storage;
using AdviseCare.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace AdviseCare.Application.Services
{
    public class AccountService : IAccountService
    {
        // How long a one-time link stays valid
        private const int LinkingTokenLifetimeDays = 7;
        private const int ResetTokenLifetimeHours = 1;
        private const long MaxAvatarBytes = 2 * 1024 * 1024;
        private const string SystemAdministratorRole = "System Administrator";

        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Faculty> _facultyRepository;
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository;
        private readonly IGenericRepository<Session> _sessionRepository;
        private readonly IActivityLogService _activityLogService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IEmailService _emailService;
        private readonly IStorageService _storageService;
        private readonly IExcelReader _excelReader;

        public AccountService(
            IGenericRepository<Account> accountRepository,
            IGenericRepository<Role> roleRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Faculty> facultyRepository,
            IGenericRepository<Studentprofile> studentProfileRepository,
            IGenericRepository<Session> sessionRepository,
            IActivityLogService activityLogService,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IGoogleTokenValidator googleTokenValidator,
            IEmailService emailService,
            IStorageService storageService,
            IExcelReader excelReader)
        {
            _accountRepository = accountRepository;
            _roleRepository = roleRepository;
            _accountRoleRepository = accountRoleRepository;
            _facultyRepository = facultyRepository;
            _studentProfileRepository = studentProfileRepository;
            _sessionRepository = sessionRepository;
            _activityLogService = activityLogService;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _googleTokenValidator = googleTokenValidator;
            _emailService = emailService;
            _storageService = storageService;
            _excelReader = excelReader;
        }

        // Get all Accounts
        public async Task<IEnumerable<GetAllAccountResponse>> GetAll()
        {
            var accounts = await _accountRepository.GetAllAsyncOrderBy(a => a.CreatedAt, false);
            var roleLookup = await GetRoleNamesLookupAsync();

            return accounts.Select(a => new GetAllAccountResponse
            {
                AccountId = a.AccountId,
                FacultyId = a.FacultyId,
                AvatarUrl = a.AvatarUrl ?? string.Empty,
                Email = a.Email,
                FullName = a.FullName,
                AuthProvider = a.AuthProvider ?? string.Empty,
                UserName = a.UserName,
                CreatedAt = a.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(a.PasswordHash),
                LinkStatus = a.LinkStatus,
                PreferredLanguage = a.PreferredLanguage,
                Status = a.Status,
                Roles = roleLookup.GetValueOrDefault(a.AccountId, new List<string>())
            });
        }

        // Get all Accounts with pagination
        public async Task<IEnumerable<GetAllAccountWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var accounts = await _accountRepository.GetAllAsyncOrderBy(a => a.CreatedAt, false);
            var roleLookup = await GetRoleNamesLookupAsync();

            return accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new GetAllAccountWithPaginationResponse
                {
                    AccountId = a.AccountId,
                    FacultyId = a.FacultyId,
                    AvatarUrl = a.AvatarUrl ?? string.Empty,
                    Email = a.Email,
                    FullName = a.FullName,
                    AuthProvider = a.AuthProvider ?? string.Empty,
                    UserName = a.UserName,
                    CreatedAt = a.CreatedAt,
                    HasPassword = !string.IsNullOrEmpty(a.PasswordHash),
                    LinkStatus = a.LinkStatus,
                    PreferredLanguage = a.PreferredLanguage,
                    Status = a.Status,
                    Roles = roleLookup.GetValueOrDefault(a.AccountId, new List<string>())
                });
        }

        // Get Account by Id
        public async Task<GetAccountByIdResponse> GetByIdAsync(Guid AccountId)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            return new GetAccountByIdResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                AuthProvider = account.AuthProvider ?? string.Empty,
                UserName = account.UserName,
                CreatedAt = account.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(account.PasswordHash),
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = await GetRoleNamesAsync(account.AccountId)
            };
        }

        // Search Accounts by email, full name, username (contains), faculty, role, link status or account status
        public async Task<List<SearchAccountResponse>> SearchAsync(SearchAccountRequest request)
        {
            var accounts = await _accountRepository.SearchAsyncOrderBy(a =>
                (string.IsNullOrWhiteSpace(request.Email) || a.Email.Contains(request.Email)) &&
                (string.IsNullOrWhiteSpace(request.FullName) || a.FullName.Contains(request.FullName)) &&
                (string.IsNullOrWhiteSpace(request.UserName) || a.UserName.Contains(request.UserName)) &&
                (request.FacultyId == null || a.FacultyId == request.FacultyId) &&
                (string.IsNullOrWhiteSpace(request.RoleName) || a.AccountRoles.Any(ar => ar.Role.RoleName == request.RoleName)) &&
                (string.IsNullOrWhiteSpace(request.LinkStatus) || a.LinkStatus == request.LinkStatus) &&
                (string.IsNullOrWhiteSpace(request.Status) || a.Status == request.Status),
                a => a.CreatedAt, false);
            var roleLookup = await GetRoleNamesLookupAsync();

            return accounts.Select(a => new SearchAccountResponse
            {
                AccountId = a.AccountId,
                FacultyId = a.FacultyId,
                AvatarUrl = a.AvatarUrl ?? string.Empty,
                Email = a.Email,
                FullName = a.FullName,
                AuthProvider = a.AuthProvider ?? string.Empty,
                UserName = a.UserName,
                CreatedAt = a.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(a.PasswordHash),
                LinkStatus = a.LinkStatus,
                PreferredLanguage = a.PreferredLanguage,
                Status = a.Status,
                Roles = roleLookup.GetValueOrDefault(a.AccountId, new List<string>())
            }).ToList();
        }

        // Create a single Account (no password, not linked yet) with its roles
        public async Task<CreateAccountResponse> CreateAsync(CreateAccountRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var userName = request.UserName.Trim();

            if (await _accountRepository.ExistsAsync(a => a.Email == email))
                throw new ConflictException($"An account with email '{email}' already exists.");
            if (await _accountRepository.ExistsAsync(a => a.UserName == userName))
                throw new ConflictException($"An account with username '{userName}' already exists.");
            if (request.FacultyId != null)
                await EnsureActiveFacultyAsync(request.FacultyId.Value);

            var roles = await ResolveRolesAsync(request.RoleNames);

            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                FacultyId = request.FacultyId,
                Email = email,
                FullName = request.FullName.Trim(),
                UserName = userName,
                CreatedAt = DateTime.UtcNow,
                LinkStatus = "notLinked",
                PreferredLanguage = "vi",
                Status = "active"
            };

            // Staged so the account and its roles commit together
            _accountRepository.PrepareCreate(account);
            foreach (var role in roles)
                _accountRoleRepository.PrepareCreate(new AccountRole { AccountId = account.AccountId, RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
            if (roles.Any(role => role.RoleName == "Student"))
            {
                _studentProfileRepository.PrepareCreate(new Studentprofile
                {
                    StudentId = account.AccountId,
                    AcademicStatus = "unknown"
                });
            }
            await _accountRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accountCreated", $"Created account {account.Email} ({string.Join(", ", roles.Select(r => r.RoleName))})");

            return new CreateAccountResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                AuthProvider = account.AuthProvider ?? string.Empty,
                UserName = account.UserName,
                CreatedAt = account.CreatedAt,
                HasPassword = false,
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = roles.Select(r => r.RoleName).ToList()
            };
        }

        // Update an Account's details; changing the email unlinks its Google account, so the linking email must be sent again
        public async Task<UpdateAccountResponse> UpdateAsync(Guid AccountId, UpdateAccountRequest request)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            var email = request.Email.Trim().ToLowerInvariant();
            var userName = request.UserName.Trim();

            if (await _accountRepository.ExistsAsync(a => a.Email == email && a.AccountId != AccountId))
                throw new ConflictException($"An account with email '{email}' already exists.");
            if (await _accountRepository.ExistsAsync(a => a.UserName == userName && a.AccountId != AccountId))
                throw new ConflictException($"An account with username '{userName}' already exists.");
            if (request.FacultyId != null && request.FacultyId != account.FacultyId)
                await EnsureActiveFacultyAsync(request.FacultyId.Value);

            if (!string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                account.GoogleId = null;
                account.LinkStatus = "notLinked";
                account.ResetToken = null;
                account.ResetTokenExpiresAt = null;
            }

            account.FacultyId = request.FacultyId;
            account.Email = email;
            account.FullName = request.FullName.Trim();
            account.UserName = userName;
            await _accountRepository.UpdateAsync(account);

            await _activityLogService.LogCurrentAccountAsync("accountUpdated", $"Updated account {account.Email}");

            return new UpdateAccountResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                AuthProvider = account.AuthProvider ?? string.Empty,
                UserName = account.UserName,
                CreatedAt = account.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(account.PasswordHash),
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = await GetRoleNamesAsync(account.AccountId)
            };
        }

        // Import Accounts from an Excel file (columns: email, fullName, userName, facultyCode, role).
        // All-or-nothing: if any row is invalid nothing is created and every invalid row is returned.
        public async Task<ImportAccountResponse> ImportAsync(ImportAccountRequest request)
        {
            var file = request.File ?? throw new ValidationException("An Excel file (.xlsx) is required.");
            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Only Excel files (.xlsx) can be imported.");

            List<ExcelRow> rows;
            try
            {
                using var stream = file.OpenReadStream();
                rows = _excelReader.ReadRows(stream);
            }
            catch (Exception)
            {
                throw new ValidationException("The file could not be read as an Excel workbook.");
            }

            var errors = new List<ImportAccountErrorResponse>();
            if (rows.Count == 0)
                errors.Add(new ImportAccountErrorResponse { RowNumber = 1, Message = "The file has no data rows under the header row." });

            var requiredColumns = new[] { "email", "fullName", "userName", "facultyCode", "role" };
            var missingColumns = rows.Count == 0 ? new List<string>() : requiredColumns.Where(c => !rows[0].Values.ContainsKey(c)).ToList();
            if (missingColumns.Count > 0)
                errors.Add(new ImportAccountErrorResponse { RowNumber = 1, Message = $"Missing column(s): {string.Join(", ", missingColumns)}." });

            if (errors.Count > 0)
                return new ImportAccountResponse { IsSuccess = false, Errors = errors };

            // Everything the rows are checked against, loaded once
            var existingAccounts = await _accountRepository.GetAllAsync();
            var existingEmails = existingAccounts.Select(a => a.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingUserNames = existingAccounts.Select(a => a.UserName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var faculties = (await _facultyRepository.GetAllAsync()).ToDictionary(f => f.FacultyCode, StringComparer.OrdinalIgnoreCase);
            var roles = (await _roleRepository.GetAllAsync()).ToDictionary(r => r.RoleName, StringComparer.OrdinalIgnoreCase);
            var fileEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileUserNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();

            var accounts = new List<(Account Account, List<Role> Roles)>();
            foreach (var row in rows)
            {
                var email = row.Values.GetValueOrDefault("email", string.Empty).Trim().ToLowerInvariant();
                var fullName = row.Values.GetValueOrDefault("fullName", string.Empty).Trim();
                var userName = row.Values.GetValueOrDefault("userName", string.Empty).Trim();
                var facultyCode = row.Values.GetValueOrDefault("facultyCode", string.Empty).Trim();
                var roleNames = row.Values.GetValueOrDefault("role", string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var rowErrors = new List<string>();
                if (string.IsNullOrEmpty(email)) rowErrors.Add("email is empty");
                else if (!emailValidator.IsValid(email) || email.Length > 255) rowErrors.Add($"email '{email}' is not valid");
                else if (existingEmails.Contains(email)) rowErrors.Add($"email '{email}' is already used by an account");
                else if (!fileEmails.Add(email)) rowErrors.Add($"email '{email}' appears more than once in the file");

                if (string.IsNullOrEmpty(fullName)) rowErrors.Add("fullName is empty");
                else if (fullName.Length > 255) rowErrors.Add("fullName is longer than 255 characters");

                if (string.IsNullOrEmpty(userName)) rowErrors.Add("userName is empty");
                else if (userName.Length > 255) rowErrors.Add("userName is longer than 255 characters");
                else if (existingUserNames.Contains(userName)) rowErrors.Add($"userName '{userName}' is already used by an account");
                else if (!fileUserNames.Add(userName)) rowErrors.Add($"userName '{userName}' appears more than once in the file");

                Faculty? faculty = null;
                if (string.IsNullOrEmpty(facultyCode)) rowErrors.Add("facultyCode is empty");
                else if (!faculties.TryGetValue(facultyCode, out faculty)) rowErrors.Add($"faculty code '{facultyCode}' does not exist");
                else if (faculty.Status != "active") rowErrors.Add($"faculty '{facultyCode}' is archived");

                var rowRoles = new List<Role>();
                if (roleNames.Length == 0) rowErrors.Add("role is empty");
                foreach (var roleName in roleNames)
                {
                    if (roles.TryGetValue(roleName, out var role)) rowRoles.Add(role);
                    else rowErrors.Add($"role '{roleName}' does not exist");
                }

                if (rowErrors.Count > 0)
                {
                    errors.Add(new ImportAccountErrorResponse { RowNumber = row.RowNumber, Message = string.Join("; ", rowErrors) + "." });
                    continue;
                }

                accounts.Add((new Account
                {
                    AccountId = Guid.NewGuid(),
                    FacultyId = faculty!.FacultyId,
                    Email = email,
                    FullName = fullName,
                    UserName = userName,
                    CreatedAt = DateTime.UtcNow,
                    LinkStatus = "notLinked",
                    PreferredLanguage = "vi",
                    Status = "active"
                }, rowRoles));
            }

            if (errors.Count > 0)
                return new ImportAccountResponse { IsSuccess = false, Errors = errors };

            // Staged so every account and role commits in one save
            foreach (var (account, accountRoles) in accounts)
            {
                _accountRepository.PrepareCreate(account);
                foreach (var role in accountRoles)
                    _accountRoleRepository.PrepareCreate(new AccountRole { AccountId = account.AccountId, RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
                if (accountRoles.Any(role => role.RoleName == "Student"))
                {
                    _studentProfileRepository.PrepareCreate(new Studentprofile
                    {
                        StudentId = account.AccountId,
                        AcademicStatus = "unknown"
                    });
                }
            }
            await _accountRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accountsImported", $"Imported {accounts.Count} account(s) from {file.FileName}");

            return new ImportAccountResponse
            {
                IsSuccess = true,
                CreatedCount = accounts.Count,
                CreatedAccountIds = accounts.Select(a => a.Account.AccountId).ToList()
            };
        }

        // Send (or resend) the account-linking email to Accounts that are not linked yet; linkStatus becomes emailSent
        public async Task<SendLinkingEmailResponse> SendLinkingEmailsAsync(SendLinkingEmailRequest request)
        {
            var response = new SendLinkingEmailResponse();
            foreach (var accountId in request.AccountIds.Distinct())
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.LinkStatus == "linked" || account.Status != "active")
                {
                    response.SkippedAccountIds.Add(accountId);
                    continue;
                }

                var token = GenerateToken();
                try
                {
                    await _emailService.SendAccountLinkingEmailAsync(account.Email, account.FullName, token);
                }
                catch (Exception)
                {
                    response.FailedAccountIds.Add(accountId);
                    continue;
                }

                // A new link replaces the old one, so only the newest email works
                account.ResetToken = HashToken(token);
                account.ResetTokenExpiresAt = DateTime.UtcNow.AddDays(LinkingTokenLifetimeDays);
                account.LinkStatus = "emailSent";
                await _accountRepository.UpdateAsync(account);
                response.SentCount++;
            }

            if (response.SentCount > 0)
                await _activityLogService.LogCurrentAccountAsync("linkingEmailsSent", $"Sent {response.SentCount} account-linking email(s)");

            return response;
        }

        // Activate Account: sets status back to active
        public async Task<bool> ActivateAsync(Guid AccountId)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            account.Status = "active";
            await _accountRepository.UpdateAsync(account);

            await _activityLogService.LogCurrentAccountAsync("accountActivated", $"Activated account {account.Email}");
            return true;
        }

        // Deactivate Account: blocks sign-in and ends all its sessions (an admin cannot deactivate their own account)
        public async Task<bool> DeactivateAsync(Guid ActorId, Guid AccountId)
        {
            if (ActorId == AccountId)
                throw new ForbiddenException("You cannot deactivate your own account.");

            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            account.Status = "deactivated";
            _accountRepository.PrepareUpdate(account);
            await StageEndSessionsAsync(AccountId, null);
            await _accountRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accountDeactivated", $"Deactivated account {account.Email}");
            return true;
        }

        // Ban Account: blocks sign-in and ends all its sessions (an admin cannot ban their own account)
        public async Task<bool> BanAsync(Guid ActorId, Guid AccountId)
        {
            if (ActorId == AccountId)
                throw new ForbiddenException("You cannot ban your own account.");

            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            account.Status = "banned";
            _accountRepository.PrepareUpdate(account);
            await StageEndSessionsAsync(AccountId, null);
            await _accountRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accountBanned", $"Banned account {account.Email}");
            return true;
        }

        // Unban Account: sets status back to active
        public async Task<bool> UnbanAsync(Guid AccountId)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            account.Status = "active";
            await _accountRepository.UpdateAsync(account);

            await _activityLogService.LogCurrentAccountAsync("accountUnbanned", $"Unbanned account {account.Email}");
            return true;
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

            EnsureActive(account);

            account.AuthProvider = "password";
            return await SignInAsync(account, request.DeviceInfo, "password");
        }

        // Log an Account in with "Sign in with Google": only an Account already linked to that Google account can sign in
        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleUser = await ValidateGoogleTokenAsync(request.IdToken);

            var account = await _accountRepository.FirstOrDefaultAsync(a => a.GoogleId == googleUser.GoogleId && a.LinkStatus == "linked")
                ?? throw new UnauthorizedException("This Google account is not linked to any AdviseCare account.");

            EnsureActive(account);

            account.AuthProvider = "google";
            return await SignInAsync(account, request.DeviceInfo, "google");
        }

        // Link the Account from the account-linking email to the user's Google account, then sign them in
        public async Task<LoginResponse> LinkGoogleAccountAsync(LinkGoogleAccountRequest request)
        {
            var tokenHash = HashToken(request.Token);
            var account = await _accountRepository.FirstOrDefaultAsync(a => a.ResetToken == tokenHash && a.LinkStatus != "linked")
                ?? throw new ValidationException("This linking link is not valid or has already been used.");

            if (account.ResetTokenExpiresAt == null || account.ResetTokenExpiresAt < DateTime.UtcNow)
                throw new ValidationException("This linking link has expired. Ask the System Administrator to send a new one.");

            EnsureActive(account);

            var googleUser = await ValidateGoogleTokenAsync(request.IdToken);
            if (!string.Equals(googleUser.Email, account.Email, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException($"The Google account {googleUser.Email} does not match the email of this AdviseCare account. Sign in with the Google account of {account.Email}.");

            if (await _accountRepository.ExistsAsync(a => a.GoogleId == googleUser.GoogleId && a.AccountId != account.AccountId))
                throw new ConflictException("This Google account is already linked to another AdviseCare account.");

            account.GoogleId = googleUser.GoogleId;
            account.LinkStatus = "linked";
            account.ResetToken = null;
            account.ResetTokenExpiresAt = null;
            account.AuthProvider = "google";

            var response = await SignInAsync(account, request.DeviceInfo, "google");
            await _activityLogService.LogAsync(account.AccountId, "accountLinked", $"Linked the Google account {googleUser.Email}");
            return response;
        }

        // Email a password-reset link; only linked, active Accounts get one, and the reply is the same either way
        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var account = await _accountRepository.FirstOrDefaultAsync(a => a.Email == email);

            // Not-linked accounts are skipped, so a reset can never overwrite a pending linking token
            if (account != null && account.LinkStatus == "linked" && account.Status == "active")
            {
                var token = GenerateToken();
                await _emailService.SendPasswordResetEmailAsync(account.Email, account.FullName, token);

                account.ResetToken = HashToken(token);
                account.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(ResetTokenLifetimeHours);
                await _accountRepository.UpdateAsync(account);
            }

            return new ForgotPasswordResponse { Message = "If a linked account uses this email, a password reset link has been sent to it." };
        }

        // Set a new password with the token from the reset email; every session of the Account is logged out
        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                throw new ValidationException("Passwords do not match.");

            var tokenHash = HashToken(request.Token);
            var account = await _accountRepository.FirstOrDefaultAsync(a => a.ResetToken == tokenHash && a.LinkStatus == "linked");
            if (account == null || account.ResetTokenExpiresAt == null || account.ResetTokenExpiresAt < DateTime.UtcNow)
                throw new ValidationException("This reset link is not valid or has expired.");

            account.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            account.ResetToken = null;
            account.ResetTokenExpiresAt = null;
            _accountRepository.PrepareUpdate(account);
            await StageEndSessionsAsync(account.AccountId, null);
            await _accountRepository.SaveAsync();

            await _activityLogService.LogAsync(account.AccountId, "passwordReset", "Reset the password through the email link");
            return new ResetPasswordResponse { Message = "Your password has been reset. Please sign in again." };
        }

        // Set a first password, or change the existing one; the old password is asked only when one exists.
        // The other sessions of the Account are logged out, the current one stays signed in.
        public async Task<ChangePasswordResponse> ChangePasswordAsync(Guid AccountId, Guid CurrentSessionId, ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                throw new ValidationException("Passwords do not match.");

            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            var hadPassword = !string.IsNullOrEmpty(account.PasswordHash);
            if (hadPassword)
            {
                if (string.IsNullOrEmpty(request.OldPassword))
                    throw new ValidationException("Old password is required.");
                if (!_passwordHasher.Verify(request.OldPassword, account.PasswordHash))
                    throw new ValidationException("Old password is incorrect.");
            }

            account.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            _accountRepository.PrepareUpdate(account);
            await StageEndSessionsAsync(AccountId, CurrentSessionId);
            await _accountRepository.SaveAsync();

            if (hadPassword)
                await _activityLogService.LogAsync(AccountId, "passwordChanged", "Changed the password");
            else
                await _activityLogService.LogAsync(AccountId, "passwordSet", "Set a first password");

            return new ChangePasswordResponse { Message = hadPassword ? "Password changed. Your other sessions have been logged out." : "Password set. You can now also sign in with your email or username and this password." };
        }

        // Get the profile of the signed-in Account
        public async Task<GetMyProfileResponse> GetMyProfileAsync(Guid AccountId)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            return new GetMyProfileResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                AuthProvider = account.AuthProvider ?? string.Empty,
                UserName = account.UserName,
                CreatedAt = account.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(account.PasswordHash),
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = await GetRoleNamesAsync(account.AccountId)
            };
        }

        // Update the signed-in Account's own settings: username and preferred language (the full name is set by the admin)
        public async Task<UpdateMyProfileResponse> UpdateMyProfileAsync(Guid AccountId, UpdateMyProfileRequest request)
        {
            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            var userName = request.UserName.Trim();
            if (await _accountRepository.ExistsAsync(a => a.UserName == userName && a.AccountId != AccountId))
                throw new ConflictException($"The username '{userName}' is already taken.");

            account.UserName = userName;
            account.PreferredLanguage = request.PreferredLanguage;
            await _accountRepository.UpdateAsync(account);

            await _activityLogService.LogAsync(AccountId, "profileUpdated", $"Set username to {account.UserName} and language to {account.PreferredLanguage}");

            return new UpdateMyProfileResponse
            {
                AccountId = account.AccountId,
                FacultyId = account.FacultyId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                AuthProvider = account.AuthProvider ?? string.Empty,
                UserName = account.UserName,
                CreatedAt = account.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(account.PasswordHash),
                LinkStatus = account.LinkStatus,
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = await GetRoleNamesAsync(account.AccountId)
            };
        }

        // Upload a new avatar for the signed-in Account to Supabase Storage (PNG, JPEG, WEBP or GIF, up to 2 MB)
        public async Task<UploadAvatarResponse> UploadAvatarAsync(Guid AccountId, UploadAvatarRequest request)
        {
            var file = request.File ?? throw new ValidationException("An image file is required.");
            if (file.Length == 0 || file.Length > MaxAvatarBytes)
                throw new ValidationException("The avatar must be an image of at most 2 MB.");

            var extension = file.ContentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => throw new ValidationException("The avatar must be a PNG, JPEG, WEBP or GIF image.")
            };

            var account = await _accountRepository.GetByIdAsync(AccountId)
                ?? throw new NotFoundException("Account not found.");

            // A new file name every time, so browsers never show a cached old avatar
            var path = $"{AccountId}/{Guid.NewGuid():N}{extension}";
            using var stream = file.OpenReadStream();
            var avatarUrl = await _storageService.UploadAvatarAsync(stream, path, file.ContentType);

            account.AvatarUrl = avatarUrl;
            await _accountRepository.UpdateAsync(account);

            await _activityLogService.LogAsync(AccountId, "avatarUpdated", "Uploaded a new avatar");
            return new UploadAvatarResponse { AvatarUrl = avatarUrl };
        }

        // Get all account-role assignments (ACCOUNT_ROLE)
        public async Task<IEnumerable<GetAllAccountRoleResponse>> GetAllAccountRoleAsync()
        {
            var accountRoles = await _accountRoleRepository.GetAllAsyncIncludeOrderBy(ar => ar.AssignedAt, true, ar => ar.Account, ar => ar.Role);

            return accountRoles.Select(ar => new GetAllAccountRoleResponse
            {
                AccountId = ar.AccountId,
                RoleId = ar.RoleId,
                FullName = ar.Account.FullName,
                RoleName = ar.Role.RoleName,
                AssignedAt = ar.AssignedAt
            });
        }

        // Get all account-role assignments (ACCOUNT_ROLE) with pagination
        public async Task<IEnumerable<GetAllAccountRoleWithPaginationResponse>> GetAllAccountRoleWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var accountRoles = await _accountRoleRepository.GetAllAsyncIncludeOrderBy(ar => ar.AssignedAt, true, ar => ar.Account, ar => ar.Role);

            return accountRoles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ar => new GetAllAccountRoleWithPaginationResponse
                {
                    AccountId = ar.AccountId,
                    RoleId = ar.RoleId,
                    FullName = ar.Account.FullName,
                    RoleName = ar.Role.RoleName,
                    AssignedAt = ar.AssignedAt
                });
        }

        // Get an account-role assignment (ACCOUNT_ROLE) by account Id and role Id
        public async Task<GetAccountRoleByIdResponse> GetAccountRoleByIdAsync(Guid AccountId, Guid RoleId)
        {
            var accountRole = await _accountRoleRepository.FirstOrDefaultAsync(ar => ar.AccountId == AccountId && ar.RoleId == RoleId, ar => ar.Account, ar => ar.Role)
                ?? throw new NotFoundException("This account does not have this role.");

            return new GetAccountRoleByIdResponse
            {
                AccountId = accountRole.AccountId,
                RoleId = accountRole.RoleId,
                FullName = accountRole.Account.FullName,
                RoleName = accountRole.Role.RoleName,
                AssignedAt = accountRole.AssignedAt
            };
        }

        // Search account-role assignments (ACCOUNT_ROLE) by account or role
        public async Task<List<SearchAccountRoleResponse>> SearchAccountRoleAsync(SearchAccountRoleRequest request)
        {
            var accountRoles = await _accountRoleRepository.SearchAsyncIncludeOrderBy(ar =>
                (request.AccountId == null || ar.AccountId == request.AccountId) &&
                (request.RoleId == null || ar.RoleId == request.RoleId),
                ar => ar.AssignedAt, true, ar => ar.Account, ar => ar.Role);

            return accountRoles.Select(ar => new SearchAccountRoleResponse
            {
                AccountId = ar.AccountId,
                RoleId = ar.RoleId,
                FullName = ar.Account.FullName,
                RoleName = ar.Role.RoleName,
                AssignedAt = ar.AssignedAt
            }).ToList();
        }

        // Give an Account a role (ACCOUNT_ROLE)
        public async Task<CreateAccountRoleResponse> CreateAccountRoleAsync(CreateAccountRoleRequest request)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId)
                ?? throw new NotFoundException("Account not found.");
            var role = await _roleRepository.GetByIdAsync(request.RoleId)
                ?? throw new NotFoundException("Role not found.");

            if (await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == request.AccountId && ar.RoleId == request.RoleId))
                throw new ConflictException($"This account already has the role '{role.RoleName}'.");

            var accountRole = new AccountRole
            {
                AccountId = request.AccountId,
                RoleId = request.RoleId,
                AssignedAt = DateTime.UtcNow
            };
            _accountRoleRepository.PrepareCreate(accountRole);
            if (role.RoleName == "Student" &&
                !await _studentProfileRepository.ExistsAsync(profile => profile.StudentId == account.AccountId))
            {
                _studentProfileRepository.PrepareCreate(new Studentprofile
                {
                    StudentId = account.AccountId,
                    AcademicStatus = "unknown"
                });
            }
            await _accountRoleRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accountRolesChanged", $"Gave {account.Email} the role {role.RoleName}");

            return new CreateAccountRoleResponse
            {
                AccountId = accountRole.AccountId,
                RoleId = accountRole.RoleId,
                FullName = account.FullName,
                RoleName = role.RoleName,
                AssignedAt = accountRole.AssignedAt
            };
        }

        // Remove a role from an Account (ACCOUNT_ROLE); the last System Administrator role cannot be removed
        public async Task<bool> DeleteAccountRoleAsync(Guid AccountId, Guid RoleId)
        {
            var accountRole = await _accountRoleRepository.FirstOrDefaultAsync(ar => ar.AccountId == AccountId && ar.RoleId == RoleId, ar => ar.Account, ar => ar.Role)
                ?? throw new NotFoundException("This account does not have this role.");

            if (accountRole.Role.RoleName == SystemAdministratorRole && await _accountRoleRepository.CountAsync(ar => ar.RoleId == RoleId) <= 1)
                throw new ForbiddenException("The last System Administrator role cannot be removed.");

            var detail = $"Removed the role {accountRole.Role.RoleName} from {accountRole.Account.Email}";
            await _accountRoleRepository.RemoveAsync(accountRole);

            await _activityLogService.LogCurrentAccountAsync("accountRolesChanged", detail);
            return true;
        }

        // Helper: opens a Session for an Account that passed sign-in, saves it together with the Account's changes, logs it and issues the JWT
        private async Task<LoginResponse> SignInAsync(Account account, string deviceInfo, string method)
        {
            var session = new Session
            {
                SessionId = Guid.NewGuid(),
                AccountId = account.AccountId,
                DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                LoginAt = DateTime.UtcNow,
                IsActive = true
            };
            _sessionRepository.PrepareCreate(session);
            _accountRepository.PrepareUpdate(account);
            await _accountRepository.SaveAsync();

            await _activityLogService.LogAsync(account.AccountId, "signedIn", $"Signed in with {method} ({session.DeviceInfo ?? "unknown device"})");

            var roles = await GetRoleNamesAsync(account.AccountId);
            return new LoginResponse
            {
                AccountId = account.AccountId,
                SessionId = session.SessionId,
                AvatarUrl = account.AvatarUrl ?? string.Empty,
                Email = account.Email,
                FullName = account.FullName,
                UserName = account.UserName,
                HasPassword = !string.IsNullOrEmpty(account.PasswordHash),
                PreferredLanguage = account.PreferredLanguage,
                Status = account.Status,
                Roles = roles,
                Token = _jwtTokenGenerator.GenerateToken(account, session.SessionId, roles)
            };
        }

        // Helper: stages ending every active Session of an Account, except the given one; the caller saves
        private async Task StageEndSessionsAsync(Guid accountId, Guid? exceptSessionId)
        {
            var sessions = await _sessionRepository.SearchAsync(s => s.AccountId == accountId && s.IsActive && s.SessionId != exceptSessionId);
            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutAt = DateTime.UtcNow;
                _sessionRepository.PrepareUpdate(session);
            }
        }

        // Helper: validates a Google ID token; an unverified token is never trusted
        private async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                return await _googleTokenValidator.ValidateAsync(idToken);
            }
            catch (Exception)
            {
                throw new UnauthorizedException("Invalid Google token.");
            }
        }

        // Helper: refuses deactivated and banned Accounts
        private static void EnsureActive(Account account)
        {
            if (account.Status != "active")
                throw new ForbiddenException($"Account is not active. Current status: {account.Status}.");
        }

        // Helper: the faculty must exist and must not be archived
        private async Task EnsureActiveFacultyAsync(Guid facultyId)
        {
            var faculty = await _facultyRepository.GetByIdAsync(facultyId)
                ?? throw new NotFoundException("Faculty not found.");
            if (faculty.Status != "active")
                throw new ValidationException($"Faculty '{faculty.FacultyCode}' is archived.");
        }

        // Helper: resolves role names to Role entities, throwing if any role does not exist
        private async Task<List<Role>> ResolveRolesAsync(List<string> roleNames)
        {
            var resolvedRoles = new List<Role>();
            foreach (var roleName in roleNames.Select(r => r.Trim()).Distinct())
            {
                var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == roleName)
                    ?? throw new ValidationException($"Role '{roleName}' does not exist.");
                resolvedRoles.Add(role);
            }

            return resolvedRoles;
        }

        // Helper: role names currently assigned to a single Account
        private async Task<List<string>> GetRoleNamesAsync(Guid accountId)
        {
            var accountRoles = await _accountRoleRepository.SearchAsyncInclude(ar => ar.AccountId == accountId, ar => ar.Role);
            return accountRoles.Select(ar => ar.Role.RoleName).ToList();
        }

        // Helper: role names for every Account, batched into a single query instead of one per account
        private async Task<Dictionary<Guid, List<string>>> GetRoleNamesLookupAsync()
        {
            var accountRoles = await _accountRoleRepository.GetAllAsyncInclude(ar => ar.Role);
            return accountRoles
                .GroupBy(ar => ar.AccountId)
                .ToDictionary(g => g.Key, g => g.Select(ar => ar.Role.RoleName).ToList());
        }

        // Helper: a random one-time token for the linking/reset links (only its hash is stored)
        private static string GenerateToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        // Helper: SHA-256 hash of a one-time token, as stored in ACCOUNTS.resetToken
        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
        }
    }
}
