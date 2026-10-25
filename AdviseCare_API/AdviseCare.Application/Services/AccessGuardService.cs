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

        public AccessGuardService(
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Accesspolicy> accessPolicyRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Consent> consentRepository,
            IGenericRepository<Accesslog> accessLogRepository)
        {
            _accountRoleRepository = accountRoleRepository;
            _accessPolicyRepository = accessPolicyRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _consentRepository = consentRepository;
            _accessLogRepository = accessLogRepository;
        }

        // Check which data attributes of one student the accessor may read; pass the case category's purpose inside a case, or null outside a case
        public async Task<List<AccessDecision>> CheckAccessAsync(Guid AccessorId, Guid StudentId, List<Guid> DataAttributeIds, Guid? ConsentPurposeId)
        {
            return await DecideAsync(AccessorId, new List<Guid> { StudentId }, DataAttributeIds, ConsentPurposeId);
        }

        // Check the same data attributes for a list of students at once (e.g. the advisor's student list with indicators), under any purpose
        public async Task<List<AccessDecision>> CheckAccessForStudentsAsync(
            Guid AccessorId,
            List<Guid> StudentIds,
            List<Guid> DataAttributeIds,
            Guid? ConsentPurposeId = null)
        {
            return await DecideAsync(AccessorId, StudentIds, DataAttributeIds, ConsentPurposeId);
        }

        // Write one access log row (one row per data attribute read; students reading their own data are not logged)
        public async Task LogAccessAsync(Guid AccessorId, Guid StudentId, Guid ConsentPurposeId, string AccessedResource)
        {
            if (AccessorId == StudentId)
                return;

            await LogAccessBatchAsync(
                AccessorId,
                new[] { new AccessLogBatchEntry(StudentId, ConsentPurposeId, AccessedResource) });
        }

        public async Task LogAccessBatchAsync(
            Guid AccessorId,
            IReadOnlyCollection<AccessLogBatchEntry> Entries)
        {
            var now = DateTime.UtcNow;
            var logs = Entries
                .Select(entry =>
                {
                    var accessedResource = entry.AccessedResource.Trim();
                    return new Accesslog
                    {
                        AccessLogId = Guid.NewGuid(),
                        AccessorId = AccessorId,
                        ConsentPurposeId = entry.ConsentPurposeId,
                        RecordedUserId = entry.StudentId,
                        AccessedResource = accessedResource.Length > 255 ? accessedResource[..255] : accessedResource,
                        AccessedAt = now,
                        IsFlagged = false,
                        Status = null
                    };
                })
                .ToList();

            if (logs.Count == 0)
            {
                return;
            }

            foreach (var log in logs)
                _accessLogRepository.PrepareCreate(log);

            await _accessLogRepository.SaveAsync();
        }

        // Helper: access is allowed when the student has a granted, unexpired consent on the attribute under a purpose that is active,
        // still covers the attribute, and has an active access policy for one of the accessor's roles
        private async Task<List<AccessDecision>> DecideAsync(Guid AccessorId, List<Guid> StudentIds, List<Guid> DataAttributeIds, Guid? ConsentPurposeId)
        {
            var studentIds = StudentIds.Distinct().ToList();
            var attributeIds = DataAttributeIds.Distinct().ToList();
            var decisions = studentIds
                .SelectMany(s => attributeIds.Select(a => new AccessDecision { StudentId = s, DataAttributeId = a }))
                .ToList();

            // Self-reads without a specified purpose retain the student-profile behavior.
            if (!ConsentPurposeId.HasValue)
            {
                foreach (var decision in decisions.Where(d => d.StudentId == AccessorId))
                    decision.IsAllowed = true;
            }

            var studentsRequiringConsent = ConsentPurposeId.HasValue
                ? studentIds
                : studentIds.Where(studentId => studentId != AccessorId).ToList();
            if (studentsRequiringConsent.Count == 0 || attributeIds.Count == 0)
                return decisions;

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
                return decisions;

            // Attributes each purpose still covers (PURPOSE_ATTRIBUTE)
            var purposes = await _consentPurposeRepository.SearchAsyncInclude(p => purposeIds.Contains(p.ConsentPurposeId), p => p.DataAttributes);
            var coveredAttributes = purposes.ToDictionary(
                p => p.ConsentPurposeId,
                p => p.DataAttributes.Select(a => a.DataAttributeId).ToHashSet());
            var purposeNames = purposes.ToDictionary(p => p.ConsentPurposeId, p => p.PurposeName);

            var now = DateTime.UtcNow;
            var consents = (await _consentRepository.SearchAsync(c =>
                    studentsRequiringConsent.Contains(c.StudentId) &&
                    attributeIds.Contains(c.DataAttributeId) &&
                    purposeIds.Contains(c.ConsentPurposeId) &&
                    c.Status == "granted" &&
                    (c.ExpiresAt == null || c.ExpiresAt > now)))
                .Where(c => coveredAttributes[c.ConsentPurposeId].Contains(c.DataAttributeId))
                .ToList();

            foreach (var decision in decisions.Where(d => studentsRequiringConsent.Contains(d.StudentId)))
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

            return decisions;
        }
    }
}
