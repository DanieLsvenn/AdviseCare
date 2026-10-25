using AdviseCare.Application.Contracts.Requests.EmergencyAccessRequest;
using AdviseCare.Application.Contracts.Responses.EmergencyAccessRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    public class EmergencyAccessRequestService : IEmergencyAccessRequestService
    {
        private readonly IGenericRepository<Emergencyaccessrequest> _emergencyAccessRequestRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public EmergencyAccessRequestService(
            IGenericRepository<Emergencyaccessrequest> emergencyAccessRequestRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _emergencyAccessRequestRepository = emergencyAccessRequestRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Emergency access requests
        public async Task<IEnumerable<GetAllEmergencyAccessRequestResponse>> GetAll()
        {
            var requests = await GetRequestsAsync(e => true);
            var now = DateTime.UtcNow;

            return requests.Select(e => new GetAllEmergencyAccessRequestResponse
            {
                EmergencyAccessRequestId = e.EmergencyAccessRequestId,
                RequestedBy = e.RequestedBy,
                StewardId = e.StewardId,
                StudentId = e.StudentId,
                RequesterFullName = e.RequestedByNavigation.FullName,
                StewardFullName = e.Steward?.FullName ?? string.Empty,
                StudentFullName = e.Student.FullName,
                Justification = e.Justification,
                RejectionReason = e.RejectionReason ?? string.Empty,
                DecidedAt = e.DecidedAt,
                RequestedAt = e.RequestedAt,
                ScopeExpiresAt = e.ScopeExpiresAt,
                IsExpired = e.Status == "approved" && e.ScopeExpiresAt <= now,
                Status = e.Status
            });
        }

        // Get all Emergency access requests with pagination
        public async Task<IEnumerable<GetAllEmergencyAccessRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = await GetRequestsAsync(e => true);
            var now = DateTime.UtcNow;

            return requests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new GetAllEmergencyAccessRequestWithPaginationResponse
                {
                    EmergencyAccessRequestId = e.EmergencyAccessRequestId,
                    RequestedBy = e.RequestedBy,
                    StewardId = e.StewardId,
                    StudentId = e.StudentId,
                    RequesterFullName = e.RequestedByNavigation.FullName,
                    StewardFullName = e.Steward?.FullName ?? string.Empty,
                    StudentFullName = e.Student.FullName,
                    Justification = e.Justification,
                    RejectionReason = e.RejectionReason ?? string.Empty,
                    DecidedAt = e.DecidedAt,
                    RequestedAt = e.RequestedAt,
                    ScopeExpiresAt = e.ScopeExpiresAt,
                    IsExpired = e.Status == "approved" && e.ScopeExpiresAt <= now,
                    Status = e.Status
                });
        }

        // Get Emergency access request by Id
        public async Task<GetEmergencyAccessRequestByIdResponse> GetByIdAsync(Guid EmergencyAccessRequestId)
        {
            var request = (await GetRequestsAsync(e => e.EmergencyAccessRequestId == EmergencyAccessRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Emergency access request not found.");
            var now = DateTime.UtcNow;

            return new GetEmergencyAccessRequestByIdResponse
            {
                EmergencyAccessRequestId = request.EmergencyAccessRequestId,
                RequestedBy = request.RequestedBy,
                StewardId = request.StewardId,
                StudentId = request.StudentId,
                RequesterFullName = request.RequestedByNavigation.FullName,
                StewardFullName = request.Steward?.FullName ?? string.Empty,
                StudentFullName = request.Student.FullName,
                Justification = request.Justification,
                RejectionReason = request.RejectionReason ?? string.Empty,
                DecidedAt = request.DecidedAt,
                RequestedAt = request.RequestedAt,
                ScopeExpiresAt = request.ScopeExpiresAt,
                IsExpired = request.Status == "approved" && request.ScopeExpiresAt <= now,
                Status = request.Status
            };
        }

        // Search Emergency access requests by requester, deciding steward, student or status
        public async Task<List<SearchEmergencyAccessRequestResponse>> SearchAsync(SearchEmergencyAccessRequestRequest request)
        {
            var requests = await GetRequestsAsync(e =>
                (request.RequestedBy == null || e.RequestedBy == request.RequestedBy) &&
                (request.StewardId == null || e.StewardId == request.StewardId) &&
                (request.StudentId == null || e.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || e.Status == request.Status));
            var now = DateTime.UtcNow;

            return requests.Select(e => new SearchEmergencyAccessRequestResponse
            {
                EmergencyAccessRequestId = e.EmergencyAccessRequestId,
                RequestedBy = e.RequestedBy,
                StewardId = e.StewardId,
                StudentId = e.StudentId,
                RequesterFullName = e.RequestedByNavigation.FullName,
                StewardFullName = e.Steward?.FullName ?? string.Empty,
                StudentFullName = e.Student.FullName,
                Justification = e.Justification,
                RejectionReason = e.RejectionReason ?? string.Empty,
                DecidedAt = e.DecidedAt,
                RequestedAt = e.RequestedAt,
                ScopeExpiresAt = e.ScopeExpiresAt,
                IsExpired = e.Status == "approved" && e.ScopeExpiresAt <= now,
                Status = e.Status
            }).ToList();
        }

        // Create Emergency access request (staff ask to see a student's data beyond consent; every Steward is notified, the first to act decides)
        public async Task<CreateEmergencyAccessRequestResponse> CreateAsync(Guid CreatorId, CreateEmergencyAccessRequestRequest request)
        {
            var student = await _accountRepository.GetByIdAsync(request.StudentId)
                ?? throw new NotFoundException("Student not found.");
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == student.AccountId && ar.Role.RoleName == "Student"))
                throw new ValidationException("Emergency access can only be requested for a student.");

            var now = DateTime.UtcNow;
            if (await _emergencyAccessRequestRepository.ExistsAsync(e =>
                    e.RequestedBy == CreatorId &&
                    e.StudentId == student.AccountId &&
                    (e.Status == "pending" || (e.Status == "approved" && e.ScopeExpiresAt > now))))
                throw new ConflictException("You already have a pending request or a running emergency access for this student.");

            var emergencyRequest = new Emergencyaccessrequest
            {
                EmergencyAccessRequestId = Guid.NewGuid(),
                RequestedBy = CreatorId,
                StewardId = null,
                StudentId = student.AccountId,
                Justification = request.Justification.Trim(),
                RequestedAt = now,
                Status = "pending"
            };
            await _emergencyAccessRequestRepository.CreateAsync(emergencyRequest);

            var requester = await _accountRepository.GetByIdAsync(CreatorId);
            await NotifyStewardsAsync("Emergency access requested",
                $"{requester?.FullName} asks for emergency access to {student.FullName}'s record.");
            await _activityLogService.LogCurrentAccountAsync("emergencyAccessRequested",
                $"Requested emergency access to {student.FullName}'s record");

            return new CreateEmergencyAccessRequestResponse
            {
                EmergencyAccessRequestId = emergencyRequest.EmergencyAccessRequestId,
                RequestedBy = emergencyRequest.RequestedBy,
                StewardId = emergencyRequest.StewardId,
                StudentId = emergencyRequest.StudentId,
                RequesterFullName = requester?.FullName ?? string.Empty,
                StewardFullName = string.Empty,
                StudentFullName = student.FullName,
                Justification = emergencyRequest.Justification,
                RejectionReason = emergencyRequest.RejectionReason ?? string.Empty,
                DecidedAt = emergencyRequest.DecidedAt,
                RequestedAt = emergencyRequest.RequestedAt,
                ScopeExpiresAt = emergencyRequest.ScopeExpiresAt,
                IsExpired = false,
                Status = emergencyRequest.Status
            };
        }

        // Update Emergency access request (the requester rewords the justification while it is pending)
        public async Task<UpdateEmergencyAccessRequestResponse> UpdateAsync(Guid RequesterId, Guid EmergencyAccessRequestId, UpdateEmergencyAccessRequestRequest request)
        {
            var emergencyRequest = (await GetRequestsAsync(e => e.EmergencyAccessRequestId == EmergencyAccessRequestId)).FirstOrDefault();
            if (emergencyRequest == null || emergencyRequest.RequestedBy != RequesterId)
                throw new NotFoundException("Emergency access request not found.");
            if (emergencyRequest.Status != "pending")
                throw new ConflictException("Only a pending request can be edited.");

            emergencyRequest.Justification = request.Justification.Trim();
            await _emergencyAccessRequestRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("emergencyAccessRequestUpdated",
                $"Edited the emergency access request for {emergencyRequest.Student.FullName}'s record");

            var now = DateTime.UtcNow;
            return new UpdateEmergencyAccessRequestResponse
            {
                EmergencyAccessRequestId = emergencyRequest.EmergencyAccessRequestId,
                RequestedBy = emergencyRequest.RequestedBy,
                StewardId = emergencyRequest.StewardId,
                StudentId = emergencyRequest.StudentId,
                RequesterFullName = emergencyRequest.RequestedByNavigation.FullName,
                StewardFullName = emergencyRequest.Steward?.FullName ?? string.Empty,
                StudentFullName = emergencyRequest.Student.FullName,
                Justification = emergencyRequest.Justification,
                RejectionReason = emergencyRequest.RejectionReason ?? string.Empty,
                DecidedAt = emergencyRequest.DecidedAt,
                RequestedAt = emergencyRequest.RequestedAt,
                ScopeExpiresAt = emergencyRequest.ScopeExpiresAt,
                IsExpired = emergencyRequest.Status == "approved" && emergencyRequest.ScopeExpiresAt <= now,
                Status = emergencyRequest.Status
            };
        }

        // Approve Emergency access request with a time limit (the first Steward to act is recorded; access ends on its own at scopeExpiresAt)
        public async Task<bool> ApproveAsync(Guid StewardId, Guid EmergencyAccessRequestId, ApproveEmergencyAccessRequestRequest request)
        {
            var emergencyRequest = await GetPendingAsync(EmergencyAccessRequestId);
            var now = DateTime.UtcNow;
            if (request.ScopeExpiresAt == null || request.ScopeExpiresAt <= now)
                throw new ValidationException("The end of the access window must be in the future.");

            emergencyRequest.StewardId = StewardId;
            emergencyRequest.DecidedAt = now;
            emergencyRequest.ScopeExpiresAt = request.ScopeExpiresAt;
            emergencyRequest.Status = "approved";
            await _emergencyAccessRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(emergencyRequest.RequestedBy, "Emergency access approved",
                $"You may access {emergencyRequest.Student.FullName}'s record until {emergencyRequest.ScopeExpiresAt:yyyy-MM-dd HH:mm} (UTC). Every access is logged.", "/emergency-access");
            await _activityLogService.LogCurrentAccountAsync("emergencyAccessDecided",
                $"Approved emergency access for {emergencyRequest.RequestedByNavigation.FullName} to {emergencyRequest.Student.FullName}'s record until {emergencyRequest.ScopeExpiresAt:yyyy-MM-dd HH:mm}");
            return true;
        }

        // Reject Emergency access request with a reason (the first Steward to act is recorded)
        public async Task<bool> RejectAsync(Guid StewardId, Guid EmergencyAccessRequestId, RejectEmergencyAccessRequestRequest request)
        {
            var emergencyRequest = await GetPendingAsync(EmergencyAccessRequestId);

            emergencyRequest.StewardId = StewardId;
            emergencyRequest.DecidedAt = DateTime.UtcNow;
            emergencyRequest.RejectionReason = request.RejectionReason.Trim();
            emergencyRequest.Status = "rejected";
            await _emergencyAccessRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(emergencyRequest.RequestedBy, "Emergency access rejected",
                $"Your emergency access request for {emergencyRequest.Student.FullName}'s record was rejected: {emergencyRequest.RejectionReason}", "/emergency-access");
            await _activityLogService.LogCurrentAccountAsync("emergencyAccessDecided",
                $"Rejected emergency access for {emergencyRequest.RequestedByNavigation.FullName} to {emergencyRequest.Student.FullName}'s record: {emergencyRequest.RejectionReason}");
            return true;
        }

        // Get the Emergency access requests a staff member sent, newest first
        public async Task<List<GetMyEmergencyAccessRequestResponse>> GetMyRequestsAsync(Guid RequesterId)
        {
            var requests = await GetRequestsAsync(e => e.RequestedBy == RequesterId);
            var now = DateTime.UtcNow;

            return requests.Select(e => new GetMyEmergencyAccessRequestResponse
            {
                EmergencyAccessRequestId = e.EmergencyAccessRequestId,
                RequestedBy = e.RequestedBy,
                StewardId = e.StewardId,
                StudentId = e.StudentId,
                RequesterFullName = e.RequestedByNavigation.FullName,
                StewardFullName = e.Steward?.FullName ?? string.Empty,
                StudentFullName = e.Student.FullName,
                Justification = e.Justification,
                RejectionReason = e.RejectionReason ?? string.Empty,
                DecidedAt = e.DecidedAt,
                RequestedAt = e.RequestedAt,
                ScopeExpiresAt = e.ScopeExpiresAt,
                IsExpired = e.Status == "approved" && e.ScopeExpiresAt <= now,
                Status = e.Status
            }).ToList();
        }

        // Helper: load Emergency access requests with requester, steward and student, newest first
        private async Task<List<Emergencyaccessrequest>> GetRequestsAsync(Expression<Func<Emergencyaccessrequest, bool>> predicate)
        {
            return await _emergencyAccessRequestRepository.SearchAsyncIncludeOrderBy(predicate, e => e.RequestedAt, false,
                e => e.RequestedByNavigation, e => e.Steward, e => e.Student);
        }

        // Helper: load a request that is still pending (stewards share one pool, so a request another Steward already decided is refused)
        private async Task<Emergencyaccessrequest> GetPendingAsync(Guid EmergencyAccessRequestId)
        {
            var emergencyRequest = (await GetRequestsAsync(e => e.EmergencyAccessRequestId == EmergencyAccessRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Emergency access request not found.");
            if (emergencyRequest.Status != "pending")
                throw new ConflictException($"This request was already {emergencyRequest.Status} by {emergencyRequest.Steward?.FullName ?? "another steward"}.");
            return emergencyRequest;
        }

        // Helper: notify every active Data Protection Steward
        private async Task NotifyStewardsAsync(string title, string message)
        {
            var stewards = await _accountRoleRepository.SearchAsync(ar => ar.Role.RoleName == "Data Protection Steward" && ar.Account.Status == "active");
            foreach (var steward in stewards)
                await _notificationService.NotifyAsync(steward.AccountId, title, message, "/emergency-access");
        }
    }
}
