using AdviseCare.Application.Contracts.Requests.Consent;
using AdviseCare.Application.Contracts.Requests.ConsentRequest;
using AdviseCare.Application.Contracts.Responses.ConsentRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    public class ConsentRequestService : IConsentRequestService
    {
        private readonly IGenericRepository<Consentrequest> _consentRequestRepository;
        private readonly IGenericRepository<Consentpurpose> _consentPurposeRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository;
        private readonly IGenericRepository<Accesspolicy> _accessPolicyRepository;
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IConsentService _consentService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public ConsentRequestService(
            IGenericRepository<Consentrequest> consentRequestRepository,
            IGenericRepository<Consentpurpose> consentPurposeRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<AccountRole> accountRoleRepository,
            IGenericRepository<Accesspolicy> accessPolicyRepository,
            IGenericRepository<Consent> consentRepository,
            IConsentService consentService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _consentRequestRepository = consentRequestRepository;
            _consentPurposeRepository = consentPurposeRepository;
            _accountRepository = accountRepository;
            _accountRoleRepository = accountRoleRepository;
            _accessPolicyRepository = accessPolicyRepository;
            _consentRepository = consentRepository;
            _consentService = consentService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Consent requests
        public async Task<IEnumerable<GetAllConsentRequestResponse>> GetAll()
        {
            var requests = await GetRequestsAsync(r => true);

            return requests.Select(r => new GetAllConsentRequestResponse
            {
                ConsentRequestId = r.ConsentRequestId,
                AdvisorId = r.AdvisorId,
                ConsentPurposeId = r.ConsentPurposeId,
                DataAttributeId = r.DataAttributeId,
                StudentId = r.StudentId,
                AdvisorFullName = r.Advisor.FullName,
                AttributeName = r.DataAttribute.AttributeName,
                PurposeName = r.ConsentPurpose.PurposeName,
                StudentFullName = r.Student.FullName,
                PurposeStated = r.PurposeStated,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt,
                Status = r.Status
            });
        }

        // Get all Consent requests with pagination
        public async Task<IEnumerable<GetAllConsentRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = await GetRequestsAsync(r => true);

            return requests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllConsentRequestWithPaginationResponse
                {
                    ConsentRequestId = r.ConsentRequestId,
                    AdvisorId = r.AdvisorId,
                    ConsentPurposeId = r.ConsentPurposeId,
                    DataAttributeId = r.DataAttributeId,
                    StudentId = r.StudentId,
                    AdvisorFullName = r.Advisor.FullName,
                    AttributeName = r.DataAttribute.AttributeName,
                    PurposeName = r.ConsentPurpose.PurposeName,
                    StudentFullName = r.Student.FullName,
                    PurposeStated = r.PurposeStated,
                    RequestedAt = r.RequestedAt,
                    RespondedAt = r.RespondedAt,
                    Status = r.Status
                });
        }

        // Get Consent request by Id
        public async Task<GetConsentRequestByIdResponse> GetByIdAsync(Guid ConsentRequestId)
        {
            var request = (await GetRequestsAsync(r => r.ConsentRequestId == ConsentRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Consent request not found.");

            return new GetConsentRequestByIdResponse
            {
                ConsentRequestId = request.ConsentRequestId,
                AdvisorId = request.AdvisorId,
                ConsentPurposeId = request.ConsentPurposeId,
                DataAttributeId = request.DataAttributeId,
                StudentId = request.StudentId,
                AdvisorFullName = request.Advisor.FullName,
                AttributeName = request.DataAttribute.AttributeName,
                PurposeName = request.ConsentPurpose.PurposeName,
                StudentFullName = request.Student.FullName,
                PurposeStated = request.PurposeStated,
                RequestedAt = request.RequestedAt,
                RespondedAt = request.RespondedAt,
                Status = request.Status
            };
        }

        // Search Consent requests by advisor, purpose, attribute, student or status
        public async Task<List<SearchConsentRequestResponse>> SearchAsync(SearchConsentRequestRequest request)
        {
            var requests = await GetRequestsAsync(r =>
                (request.AdvisorId == null || r.AdvisorId == request.AdvisorId) &&
                (request.ConsentPurposeId == null || r.ConsentPurposeId == request.ConsentPurposeId) &&
                (request.DataAttributeId == null || r.DataAttributeId == request.DataAttributeId) &&
                (request.StudentId == null || r.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status));

            return requests.Select(r => new SearchConsentRequestResponse
            {
                ConsentRequestId = r.ConsentRequestId,
                AdvisorId = r.AdvisorId,
                ConsentPurposeId = r.ConsentPurposeId,
                DataAttributeId = r.DataAttributeId,
                StudentId = r.StudentId,
                AdvisorFullName = r.Advisor.FullName,
                AttributeName = r.DataAttribute.AttributeName,
                PurposeName = r.ConsentPurpose.PurposeName,
                StudentFullName = r.Student.FullName,
                PurposeStated = r.PurposeStated,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt,
                Status = r.Status
            }).ToList();
        }

        // Create Consent request (an advisor asks a student to share one attribute under one purpose)
        public async Task<CreateConsentRequestResponse> CreateAsync(Guid CreatorId, CreateConsentRequestRequest request)
        {
            var student = await _accountRepository.GetByIdAsync(request.StudentId)
                ?? throw new NotFoundException("Student not found.");
            if (!await _accountRoleRepository.ExistsAsync(ar => ar.AccountId == student.AccountId && ar.Role.RoleName == "Student"))
                throw new ValidationException("Consent can only be requested from a student.");

            var purpose = await _consentPurposeRepository.GetByIdAsyncInclude(request.ConsentPurposeId, p => p.DataAttributes)
                ?? throw new NotFoundException("Consent purpose not found.");
            if (purpose.Status != "active")
                throw new ConflictException($"'{purpose.PurposeName}' is not active.");
            var attribute = purpose.DataAttributes.FirstOrDefault(a => a.DataAttributeId == request.DataAttributeId)
                ?? throw new ValidationException($"This data attribute is not covered by '{purpose.PurposeName}'.");
            if (attribute.Status != "active")
                throw new ConflictException($"'{attribute.AttributeName}' is archived and cannot be requested.");

            // A granted request must actually let the advisor see the value, so one of the advisor's roles needs an active policy for the purpose
            var roleIds = (await _accountRoleRepository.SearchAsync(ar => ar.AccountId == CreatorId)).Select(ar => ar.RoleId).ToList();
            if (!await _accessPolicyRepository.ExistsAsync(p => p.ConsentPurposeId == purpose.ConsentPurposeId && roleIds.Contains(p.RoleId) && p.Status == "active"))
                throw new ValidationException($"None of your roles has an active access policy under '{purpose.PurposeName}', so a granted request would not give you access.");

            var now = DateTime.UtcNow;
            if (await _consentRepository.ExistsAsync(c =>
                    c.StudentId == student.AccountId &&
                    c.ConsentPurposeId == purpose.ConsentPurposeId &&
                    c.DataAttributeId == attribute.DataAttributeId &&
                    c.Status == "granted" &&
                    (c.ExpiresAt == null || c.ExpiresAt > now)))
                throw new ConflictException($"The student already shares '{attribute.AttributeName}' under '{purpose.PurposeName}'.");
            if (await _consentRequestRepository.ExistsAsync(r =>
                    r.AdvisorId == CreatorId &&
                    r.StudentId == student.AccountId &&
                    r.ConsentPurposeId == purpose.ConsentPurposeId &&
                    r.DataAttributeId == attribute.DataAttributeId &&
                    r.Status == "pending"))
                throw new ConflictException("You already have a pending request for this attribute.");

            var consentRequest = new Consentrequest
            {
                ConsentRequestId = Guid.NewGuid(),
                AdvisorId = CreatorId,
                ConsentPurposeId = purpose.ConsentPurposeId,
                DataAttributeId = attribute.DataAttributeId,
                StudentId = student.AccountId,
                PurposeStated = request.PurposeStated.Trim(),
                RequestedAt = now,
                Status = "pending"
            };
            await _consentRequestRepository.CreateAsync(consentRequest);

            var advisor = await _accountRepository.GetByIdAsync(CreatorId);
            await _notificationService.NotifyAsync(student.AccountId, "New consent request",
                $"{advisor?.FullName} asks you to share {attribute.AttributeName} for {purpose.PurposeName}: {consentRequest.PurposeStated}", "/consents/requests");
            await _activityLogService.LogCurrentAccountAsync("consentRequested",
                $"Asked {student.FullName} to share {attribute.AttributeName} under {purpose.PurposeName}");

            return new CreateConsentRequestResponse
            {
                ConsentRequestId = consentRequest.ConsentRequestId,
                AdvisorId = consentRequest.AdvisorId,
                ConsentPurposeId = consentRequest.ConsentPurposeId,
                DataAttributeId = consentRequest.DataAttributeId,
                StudentId = consentRequest.StudentId,
                AdvisorFullName = advisor?.FullName ?? string.Empty,
                AttributeName = attribute.AttributeName,
                PurposeName = purpose.PurposeName,
                StudentFullName = student.FullName,
                PurposeStated = consentRequest.PurposeStated,
                RequestedAt = consentRequest.RequestedAt,
                RespondedAt = consentRequest.RespondedAt,
                Status = consentRequest.Status
            };
        }

        // Update Consent request (the advisor rewords the stated purpose of an own pending request)
        public async Task<UpdateConsentRequestResponse> UpdateAsync(Guid AdvisorId, Guid ConsentRequestId, UpdateConsentRequestRequest request)
        {
            var consentRequest = await GetOwnRequestAsync(ConsentRequestId, r => r.AdvisorId == AdvisorId);
            if (consentRequest.Status != "pending")
                throw new ConflictException("Only a pending request can be edited.");

            consentRequest.PurposeStated = request.PurposeStated.Trim();
            await _consentRequestRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentRequestUpdated",
                $"Edited the consent request to {consentRequest.Student.FullName} for {consentRequest.DataAttribute.AttributeName}");

            return new UpdateConsentRequestResponse
            {
                ConsentRequestId = consentRequest.ConsentRequestId,
                AdvisorId = consentRequest.AdvisorId,
                ConsentPurposeId = consentRequest.ConsentPurposeId,
                DataAttributeId = consentRequest.DataAttributeId,
                StudentId = consentRequest.StudentId,
                AdvisorFullName = consentRequest.Advisor.FullName,
                AttributeName = consentRequest.DataAttribute.AttributeName,
                PurposeName = consentRequest.ConsentPurpose.PurposeName,
                StudentFullName = consentRequest.Student.FullName,
                PurposeStated = consentRequest.PurposeStated,
                RequestedAt = consentRequest.RequestedAt,
                RespondedAt = consentRequest.RespondedAt,
                Status = consentRequest.Status
            };
        }

        // Grant Consent request (the student shares the attribute; a consent row is created under the request's purpose)
        public async Task<bool> GrantAsync(Guid StudentId, Guid ConsentRequestId)
        {
            var consentRequest = await GetOwnRequestAsync(ConsentRequestId, r => r.StudentId == StudentId);
            if (consentRequest.Status != "pending")
                throw new ConflictException("This request has already been answered or withdrawn.");

            await _consentService.CreateAsync(StudentId, new CreateConsentRequest
            {
                ConsentPurposeId = consentRequest.ConsentPurposeId,
                DataAttributeIds = new List<Guid> { consentRequest.DataAttributeId },
                ExpiresAt = null
            });

            consentRequest.Status = "granted";
            consentRequest.RespondedAt = DateTime.UtcNow;
            await _consentRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(consentRequest.AdvisorId, "Consent request granted",
                $"{consentRequest.Student.FullName} shared {consentRequest.DataAttribute.AttributeName} for {consentRequest.ConsentPurpose.PurposeName}.", "/consents/requests/sent");
            await _activityLogService.LogCurrentAccountAsync("consentRequestAnswered",
                $"Granted the request of {consentRequest.Advisor.FullName} for {consentRequest.DataAttribute.AttributeName}");
            return true;
        }

        // Deny Consent request (the advisor keeps seeing the attribute as "exists, not shared")
        public async Task<bool> DenyAsync(Guid StudentId, Guid ConsentRequestId)
        {
            var consentRequest = await GetOwnRequestAsync(ConsentRequestId, r => r.StudentId == StudentId);
            if (consentRequest.Status != "pending")
                throw new ConflictException("This request has already been answered or withdrawn.");

            consentRequest.Status = "denied";
            consentRequest.RespondedAt = DateTime.UtcNow;
            await _consentRequestRepository.SaveAsync();

            await _notificationService.NotifyAsync(consentRequest.AdvisorId, "Consent request denied",
                $"{consentRequest.Student.FullName} did not share {consentRequest.DataAttribute.AttributeName} for {consentRequest.ConsentPurpose.PurposeName}.", "/consents/requests/sent");
            await _activityLogService.LogCurrentAccountAsync("consentRequestAnswered",
                $"Denied the request of {consentRequest.Advisor.FullName} for {consentRequest.DataAttribute.AttributeName}");
            return true;
        }

        // Withdraw Consent request (the advisor takes back an own pending request)
        public async Task<bool> WithdrawAsync(Guid AdvisorId, Guid ConsentRequestId)
        {
            var consentRequest = await GetOwnRequestAsync(ConsentRequestId, r => r.AdvisorId == AdvisorId);
            if (consentRequest.Status != "pending")
                throw new ConflictException("Only a pending request can be withdrawn.");

            consentRequest.Status = "withdrawn";
            await _consentRequestRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("consentRequestWithdrawn",
                $"Withdrew the consent request to {consentRequest.Student.FullName} for {consentRequest.DataAttribute.AttributeName}");
            return true;
        }

        // Get the consent requests a Student received, newest first
        public async Task<List<GetMyConsentRequestResponse>> GetMyReceivedRequestsAsync(Guid StudentId)
        {
            var requests = await GetRequestsAsync(r => r.StudentId == StudentId);
            return requests.Select(r => new GetMyConsentRequestResponse
            {
                ConsentRequestId = r.ConsentRequestId,
                AdvisorId = r.AdvisorId,
                ConsentPurposeId = r.ConsentPurposeId,
                DataAttributeId = r.DataAttributeId,
                StudentId = r.StudentId,
                AdvisorFullName = r.Advisor.FullName,
                AttributeName = r.DataAttribute.AttributeName,
                PurposeName = r.ConsentPurpose.PurposeName,
                StudentFullName = r.Student.FullName,
                PurposeStated = r.PurposeStated,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt,
                Status = r.Status
            }).ToList();
        }

        // Get the consent requests an advisor sent, newest first
        public async Task<List<GetMyConsentRequestResponse>> GetMySentRequestsAsync(Guid AdvisorId)
        {
            var requests = await GetRequestsAsync(r => r.AdvisorId == AdvisorId);
            return requests.Select(r => new GetMyConsentRequestResponse
            {
                ConsentRequestId = r.ConsentRequestId,
                AdvisorId = r.AdvisorId,
                ConsentPurposeId = r.ConsentPurposeId,
                DataAttributeId = r.DataAttributeId,
                StudentId = r.StudentId,
                AdvisorFullName = r.Advisor.FullName,
                AttributeName = r.DataAttribute.AttributeName,
                PurposeName = r.ConsentPurpose.PurposeName,
                StudentFullName = r.Student.FullName,
                PurposeStated = r.PurposeStated,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt,
                Status = r.Status
            }).ToList();
        }

        // Helper: load Consent requests with the advisor, purpose, attribute and student, newest first
        private async Task<List<Consentrequest>> GetRequestsAsync(Expression<Func<Consentrequest, bool>> predicate)
        {
            return await _consentRequestRepository.SearchAsyncIncludeOrderBy(predicate, r => r.RequestedAt, false,
                r => r.Advisor, r => r.ConsentPurpose, r => r.DataAttribute, r => r.Student);
        }

        // Helper: load one Consent request that belongs to the caller (404 for anyone else)
        private async Task<Consentrequest> GetOwnRequestAsync(Guid ConsentRequestId, Func<Consentrequest, bool> isOwner)
        {
            var consentRequest = (await GetRequestsAsync(r => r.ConsentRequestId == ConsentRequestId)).FirstOrDefault();
            if (consentRequest == null || !isOwner(consentRequest))
                throw new NotFoundException("Consent request not found.");
            return consentRequest;
        }
    }
}
