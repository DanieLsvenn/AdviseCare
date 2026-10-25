using AdviseCare.Application.DTOs;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class AccessGuardService : IAccessGuardService
    {
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Accesspolicy> _accessPolicyRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IGenericRepository<Accesslog> _accessLogRepository;
        private readonly IGenericRepository<Emergencyaccessrequest> _emergencyAccessRequestRepository;

        public AccessGuardService(
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Accesspolicy> accessPolicyRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Consent> consentRepository,
            IGenericRepository<Accesslog> accessLogRepository,
            IGenericRepository<Emergencyaccessrequest> emergencyAccessRequestRepository)
        {
            _accountRoleRepository = accountRoleRepository;
            _accessPolicyRepository = accessPolicyRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _consentRepository = consentRepository;
            _accessLogRepository = accessLogRepository;
            _emergencyAccessRequestRepository = emergencyAccessRequestRepository;
        }

        // Check which data attributes of one student the accessor may read; pass the case category's purpose inside a case, or null outside a case
        public async Task<List<AccessDecision>> CheckAccessAsync(Guid AccessorId, Guid StudentId, List<Guid> DataAttributeIds, Guid? ConsentPurposeId)
        {
            return await DecideAsync(AccessorId, new List<Guid> { StudentId }, DataAttributeIds, ConsentPurposeId);
        }

        // Check the same data attributes for a list of students at once (e.g. the advisor's student list with indicators), under any purpose
        public async Task<List<AccessDecision>> CheckAccessForStudentsAsync(Guid AccessorId, List<Guid> StudentIds, List<Guid> DataAttributeIds)
        {
            return await DecideAsync(AccessorId, StudentIds, DataAttributeIds, null);
        }

        // Write one access log row (one row per data attribute read; consentPurposeId = null for an emergency access read; students reading their own data are not logged)
        public async Task LogAccessAsync(Guid AccessorId, Guid StudentId, Guid? ConsentPurposeId, string AccessedResource)
        {
            if (AccessorId == StudentId)
                return;

            var accessedResource = AccessedResource.Trim();
            await _accessLogRepository.CreateAsync(new Accesslog
            {
                AccessLogId = Guid.NewGuid(),
                AccessorId = AccessorId,
                ConsentPurposeId = ConsentPurposeId,
                RecordedUserId = StudentId,
                AccessedResource = accessedResource.Length > 255 ? accessedResource[..255] : accessedResource,
                AccessedAt = DateTime.UtcNow,
                IsFlagged = false,
                Status = null
            });
        }

        // Helper: access is allowed when the student has a granted, unexpired consent on the attribute under a purpose that is active,
        // still covers the attribute, and has an active access policy for one of the accessor's roles; otherwise it is allowed only
        // through an approved, unexpired emergency access request of the accessor for that student (every attribute, no purpose)
        private async Task<List<AccessDecision>> DecideAsync(Guid AccessorId, List<Guid> StudentIds, List<Guid> DataAttributeIds, Guid? ConsentPurposeId)
        {
            var studentIds = StudentIds.Distinct().ToList();
            var attributeIds = DataAttributeIds.Distinct().ToList();
            var decisions = studentIds
                .SelectMany(s => attributeIds.Select(a => new AccessDecision { StudentId = s, DataAttributeId = a }))
                .ToList();

            // Students always see their own data
            foreach (var decision in decisions.Where(d => d.StudentId == AccessorId))
                decision.IsAllowed = true;

            var otherStudentIds = studentIds.Where(s => s != AccessorId).ToList();
            if (otherStudentIds.Count == 0 || attributeIds.Count == 0)
                return decisions;

            await ApplyConsentsAsync(AccessorId, otherStudentIds, attributeIds, ConsentPurposeId, decisions);
            await ApplyEmergencyAccessAsync(AccessorId, decisions.Where(d => !d.IsAllowed).ToList());
            return decisions;
        }

        // Helper: allow the decisions covered by a consent under a purpose the accessor's roles have an active policy for
        private async Task ApplyConsentsAsync(Guid AccessorId, List<Guid> otherStudentIds, List<Guid> attributeIds, Guid? ConsentPurposeId, List<AccessDecision> decisions)
        {
            var roleIds = (await _accountRoleRepository.SearchAsync(ar => ar.AccountId == AccessorId))
                .Select(ar => ar.RoleId)
                .ToList();
            var purposeIds = (await _accessPolicyRepository.SearchAsync(p =>
                    p.Status == "active" &&
                    roleIds.Contains(p.RoleId) &&
                    p.ConsentPurpose.Status == "active" &&
                    (ConsentPurposeId == null || p.ConsentPurposeId == ConsentPurposeId)))
                .Select(p => p.ConsentPurposeId)
                .Distinct()
                .ToList();
            if (purposeIds.Count == 0)
                return;

            // Attributes each purpose still covers (PURPOSE_ATTRIBUTE)
            var purposes = await _consentPurposeRepository.SearchAsyncInclude(p => purposeIds.Contains(p.ConsentPurposeId), p => p.DataAttributes);
            var coveredAttributes = purposes.ToDictionary(
                p => p.ConsentPurposeId,
                p => p.DataAttributes.Select(a => a.DataAttributeId).ToHashSet());
            var purposeNames = purposes.ToDictionary(p => p.ConsentPurposeId, p => p.PurposeName);

            var now = DateTime.UtcNow;
            var consents = (await _consentRepository.SearchAsync(c =>
                    otherStudentIds.Contains(c.StudentId) &&
                    attributeIds.Contains(c.DataAttributeId) &&
                    purposeIds.Contains(c.ConsentPurposeId) &&
                    c.Status == "granted" &&
                    (c.ExpiresAt == null || c.ExpiresAt > now)))
                .Where(c => coveredAttributes[c.ConsentPurposeId].Contains(c.DataAttributeId))
                .ToList();

            foreach (var decision in decisions.Where(d => d.StudentId != AccessorId))
            {
                var consent = consents
                    .Where(c => c.StudentId == decision.StudentId && c.DataAttributeId == decision.DataAttributeId)
                    .OrderBy(c => purposeNames[c.ConsentPurposeId])
                    .FirstOrDefault();
                if (consent != null)
                {
                    decision.IsAllowed = true;
                    decision.ConsentPurposeId = consent.ConsentPurposeId;
                }
            }
        }

        // Helper: allow the remaining decisions of students the accessor has an approved, unexpired emergency access for
        private async Task ApplyEmergencyAccessAsync(Guid AccessorId, List<AccessDecision> refused)
        {
            if (refused.Count == 0)
                return;

            var studentIds = refused.Select(d => d.StudentId).Distinct().ToList();
            var now = DateTime.UtcNow;
            var emergencyStudentIds = (await _emergencyAccessRequestRepository.SearchAsync(e =>
                    e.RequestedBy == AccessorId &&
                    studentIds.Contains(e.StudentId) &&
                    e.Status == "approved" &&
                    e.ScopeExpiresAt > now))
                .Select(e => e.StudentId)
                .ToHashSet();

            foreach (var decision in refused.Where(d => emergencyStudentIds.Contains(d.StudentId)))
            {
                decision.IsAllowed = true;
                decision.IsEmergencyAccess = true;
                decision.ConsentPurposeId = null;
            }
        }
    }
}
