using AdviseCare.Application.Contracts.Requests.AccessPolicy;
using AdviseCare.Application.Contracts.Responses.AccessPolicy;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    public class AccessPolicyService : IAccessPolicyService
    {
        private readonly IGenericRepository<Accesspolicy> _accessPolicyRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IGenericRepository<Studentindicator> _studentIndicatorRepository;
        private readonly IActivityLogService _activityLogService;

        public AccessPolicyService(
            IGenericRepository<Accesspolicy> accessPolicyRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Role> roleRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Consent> consentRepository,
            IGenericRepository<Studentindicator> studentIndicatorRepository,
            IActivityLogService activityLogService)
        {
            _accessPolicyRepository = accessPolicyRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _roleRepository = roleRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _consentRepository = consentRepository;
            _studentIndicatorRepository = studentIndicatorRepository;
            _activityLogService = activityLogService;
        }

        // Get all Access policies
        public async Task<IEnumerable<GetAllAccessPolicyResponse>> GetAll()
        {
            var policies = await GetPoliciesAsync(p => true);

            return policies.Select(p => new GetAllAccessPolicyResponse
            {
                AccessPolicyId = p.AccessPolicyId,
                ConsentPurposeId = p.ConsentPurposeId,
                CreatedBy = p.CreatedBy,
                RoleId = p.RoleId,
                CreatorFullName = p.CreatedByNavigation.FullName,
                PurposeName = p.ConsentPurpose.PurposeName,
                RoleName = p.Role.RoleName,
                Status = p.Status
            });
        }

        // Get all Access policies with pagination
        public async Task<IEnumerable<GetAllAccessPolicyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var policies = await GetPoliciesAsync(p => true);

            return policies
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetAllAccessPolicyWithPaginationResponse
                {
                    AccessPolicyId = p.AccessPolicyId,
                    ConsentPurposeId = p.ConsentPurposeId,
                    CreatedBy = p.CreatedBy,
                    RoleId = p.RoleId,
                    CreatorFullName = p.CreatedByNavigation.FullName,
                    PurposeName = p.ConsentPurpose.PurposeName,
                    RoleName = p.Role.RoleName,
                    Status = p.Status
                });
        }

        // Get Access policy by Id
        public async Task<GetAccessPolicyByIdResponse> GetByIdAsync(Guid AccessPolicyId)
        {
            var policy = await _accessPolicyRepository.GetByIdAsyncInclude(AccessPolicyId, p => p.ConsentPurpose, p => p.CreatedByNavigation, p => p.Role)
                ?? throw new NotFoundException("Access policy not found.");

            return new GetAccessPolicyByIdResponse
            {
                AccessPolicyId = policy.AccessPolicyId,
                ConsentPurposeId = policy.ConsentPurposeId,
                CreatedBy = policy.CreatedBy,
                RoleId = policy.RoleId,
                CreatorFullName = policy.CreatedByNavigation.FullName,
                PurposeName = policy.ConsentPurpose.PurposeName,
                RoleName = policy.Role.RoleName,
                Status = policy.Status
            };
        }

        // Search Access policies by purpose, role or status
        public async Task<List<SearchAccessPolicyResponse>> SearchAsync(SearchAccessPolicyRequest request)
        {
            var policies = await GetPoliciesAsync(p =>
                (request.ConsentPurposeId == null || p.ConsentPurposeId == request.ConsentPurposeId) &&
                (request.RoleId == null || p.RoleId == request.RoleId) &&
                (string.IsNullOrWhiteSpace(request.Status) || p.Status == request.Status));

            return policies.Select(p => new SearchAccessPolicyResponse
            {
                AccessPolicyId = p.AccessPolicyId,
                ConsentPurposeId = p.ConsentPurposeId,
                CreatedBy = p.CreatedBy,
                RoleId = p.RoleId,
                CreatorFullName = p.CreatedByNavigation.FullName,
                PurposeName = p.ConsentPurpose.PurposeName,
                RoleName = p.Role.RoleName,
                Status = p.Status
            }).ToList();
        }

        // Create Access policy (starts as a draft; one live policy per role and purpose)
        public async Task<CreateAccessPolicyResponse> CreateAsync(Guid CreatorId, CreateAccessPolicyRequest request)
        {
            var (purpose, role) = await ValidatePolicyTargetAsync(request.ConsentPurposeId, request.RoleId, null);

            var policy = new Accesspolicy
            {
                AccessPolicyId = Guid.NewGuid(),
                ConsentPurposeId = purpose.ConsentPurposeId,
                CreatedBy = CreatorId,
                RoleId = role.RoleId,
                Status = "draft"
            };
            await _accessPolicyRepository.CreateAsync(policy);

            await _activityLogService.LogCurrentAccountAsync("accessPolicyCreated", $"Created access policy: {role.RoleName} under {purpose.PurposeName} (draft)");

            var creator = await _accountRepository.GetByIdAsync(CreatorId);
            return new CreateAccessPolicyResponse
            {
                AccessPolicyId = policy.AccessPolicyId,
                ConsentPurposeId = policy.ConsentPurposeId,
                CreatedBy = policy.CreatedBy,
                RoleId = policy.RoleId,
                CreatorFullName = creator?.FullName ?? string.Empty,
                PurposeName = purpose.PurposeName,
                RoleName = role.RoleName,
                Status = policy.Status
            };
        }

        // Update Access policy (only a draft can be edited; an active policy is revoked and replaced instead)
        public async Task<UpdateAccessPolicyResponse> UpdateAsync(Guid AccessPolicyId, UpdateAccessPolicyRequest request)
        {
            var policy = await _accessPolicyRepository.GetByIdAsyncInclude(AccessPolicyId, p => p.CreatedByNavigation)
                ?? throw new NotFoundException("Access policy not found.");

            if (policy.Status != "draft")
                throw new ConflictException("Only a draft access policy can be edited. Revoke it and create a new one instead.");

            var (purpose, role) = await ValidatePolicyTargetAsync(request.ConsentPurposeId, request.RoleId, AccessPolicyId);

            policy.ConsentPurposeId = purpose.ConsentPurposeId;
            policy.RoleId = role.RoleId;
            await _accessPolicyRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accessPolicyUpdated", $"Edited draft access policy: {role.RoleName} under {purpose.PurposeName}");

            return new UpdateAccessPolicyResponse
            {
                AccessPolicyId = policy.AccessPolicyId,
                ConsentPurposeId = policy.ConsentPurposeId,
                CreatedBy = policy.CreatedBy,
                RoleId = policy.RoleId,
                CreatorFullName = policy.CreatedByNavigation.FullName,
                PurposeName = purpose.PurposeName,
                RoleName = role.RoleName,
                Status = policy.Status
            };
        }

        // Activate Access policy (draft -> active)
        public async Task<bool> ActivateAsync(Guid AccessPolicyId)
        {
            var policy = await _accessPolicyRepository.GetByIdAsyncInclude(AccessPolicyId, p => p.ConsentPurpose, p => p.Role)
                ?? throw new NotFoundException("Access policy not found.");

            if (policy.Status != "draft")
                throw new ConflictException("Only a draft access policy can be activated.");
            if (policy.ConsentPurpose.Status == "retired")
                throw new ConflictException("The purpose of this policy is retired.");

            policy.Status = "active";
            await _accessPolicyRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accessPolicyActivated", $"Activated access policy: {policy.Role.RoleName} under {policy.ConsentPurpose.PurposeName}");
            return true;
        }

        // Revoke Access policy (draft or active -> revoked; one-way, policies are never deleted)
        public async Task<bool> RevokeAsync(Guid AccessPolicyId)
        {
            var policy = await _accessPolicyRepository.GetByIdAsyncInclude(AccessPolicyId, p => p.ConsentPurpose, p => p.Role)
                ?? throw new NotFoundException("Access policy not found.");

            if (policy.Status == "revoked")
                throw new ConflictException("This access policy is already revoked.");

            policy.Status = "revoked";
            await _accessPolicyRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("accessPolicyRevoked", $"Revoked access policy: {policy.Role.RoleName} under {policy.ConsentPurpose.PurposeName}");
            return true;
        }

        // Test Access policy against a sample student (read-only, nothing is written to the access log)
        public async Task<TestAccessPolicyResponse> TestAsync(Guid AccessPolicyId, TestAccessPolicyRequest request)
        {
            var policy = await _accessPolicyRepository.GetByIdAsyncInclude(AccessPolicyId, p => p.ConsentPurpose, p => p.Role)
                ?? throw new NotFoundException("Access policy not found.");
            var student = await _accountRepository.GetByIdAsync(request.StudentId)
                ?? throw new NotFoundException("Student not found.");
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == student.AccountId && ar.Role.RoleName == "Student"))
                throw new ValidationException("The sample account is not a student.");

            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(policy.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            var attributeIds = purpose.DataAttributes.Select(a => a.DataAttributeId).ToList();

            var now = DateTime.UtcNow;
            var consentedAttributeIds = (await _consentRepository.SearchAsync(c =>
                    c.StudentId == student.AccountId &&
                    c.ConsentPurposeId == purpose.ConsentPurposeId &&
                    c.Status == "granted" &&
                    (c.ExpiresAt == null || c.ExpiresAt > now)))
                .Select(c => c.DataAttributeId)
                .ToHashSet();
            var attributeIdsWithValue = (await _studentIndicatorRepository.SearchAsyncInclude(si =>
                    si.StudentId == student.AccountId &&
                    !si.IsMissing &&
                    attributeIds.Contains(si.Indicator.DataAttributeId),
                    si => si.Indicator))
                .Select(si => si.Indicator.DataAttributeId)
                .ToHashSet();

            var isInEffect = policy.Status == "active" && purpose.Status == "active";
            return new TestAccessPolicyResponse
            {
                AccessPolicyId = policy.AccessPolicyId,
                StudentId = student.AccountId,
                PurposeName = purpose.PurposeName,
                RoleName = policy.Role.RoleName,
                StudentFullName = student.FullName,
                Attributes = purpose.DataAttributes
                    .OrderBy(a => a.AttributeName)
                    .Select(a => new TestAccessPolicyAttributeResponse
                    {
                        DataAttributeId = a.DataAttributeId,
                        AttributeName = a.AttributeName,
                        HasConsent = consentedAttributeIds.Contains(a.DataAttributeId),
                        HasValue = attributeIdsWithValue.Contains(a.DataAttributeId),
                        Result = !consentedAttributeIds.Contains(a.DataAttributeId)
                            ? "notShared"
                            : isInEffect ? "allowed" : "allowedOnceActive"
                    }).ToList(),
                IsInEffect = isInEffect,
                PolicyStatus = policy.Status,
                PurposeStatus = purpose.Status
            };
        }

        // Helper: load Access policies with their purpose, creator and role, ordered by role then purpose
        private async Task<List<Accesspolicy>> GetPoliciesAsync(Expression<Func<Accesspolicy, bool>> predicate)
        {
            var policies = await _accessPolicyRepository.SearchAsyncInclude(predicate, p => p.ConsentPurpose, p => p.CreatedByNavigation, p => p.Role);
            return policies
                .OrderBy(p => p.Role.RoleName)
                .ThenBy(p => p.ConsentPurpose.PurposeName)
                .ToList();
        }

        // Helper: the purpose must not be retired, the role must exist and not be Student, and no other live (draft/active) policy may bind the same pair
        private async Task<(Consentpurpose Purpose, Role Role)> ValidatePolicyTargetAsync(Guid ConsentPurposeId, Guid RoleId, Guid? ExcludedPolicyId)
        {
            var purpose = await _consentPurposeRepository.GetByIdAsync(ConsentPurposeId)
                ?? throw new NotFoundException("Consent purpose not found.");
            var role = await _roleRepository.GetByIdAsync(RoleId)
                ?? throw new NotFoundException("Role not found.");

            if (purpose.Status == "retired")
                throw new ConflictException("A retired consent purpose cannot get new access policies.");
            if (role.RoleName == "Student")
                throw new ValidationException("Students always see their own data, so no access policy is needed for the Student role.");
            if (await _accessPolicyRepository.ExistsAsync(p =>
                    p.RoleId == RoleId &&
                    p.ConsentPurposeId == ConsentPurposeId &&
                    p.Status != "revoked" &&
                    (ExcludedPolicyId == null || p.AccessPolicyId != ExcludedPolicyId)))
                throw new ConflictException($"{role.RoleName} already has a draft or active policy under {purpose.PurposeName}.");

            return (purpose, role);
        }
    }
}
