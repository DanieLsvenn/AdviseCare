using AdviseCare.Application.Contracts.Requests.ErasureRequest;
using AdviseCare.Application.Contracts.Responses.ErasureRequest;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    public class ErasureRequestService : IErasureRequestService
    {
        private readonly IGenericRepository<Erasurerequest> _erasureRequestRepository;
        private readonly IGenericRepository<Consent> _consentRepository;
        private readonly IErasureWorkflowRepository _erasureWorkflowRepository;
        private readonly IKeyManagementService _keyManagementService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public ErasureRequestService(
            IGenericRepository<Erasurerequest> erasureRequestRepository,
            IGenericRepository<Consent> consentRepository,
            IErasureWorkflowRepository erasureWorkflowRepository,
            IKeyManagementService keyManagementService,
            INotificationService notificationService,
            IActivityLogService activityLogService)
        {
            _erasureRequestRepository = erasureRequestRepository;
            _consentRepository = consentRepository;
            _erasureWorkflowRepository = erasureWorkflowRepository;
            _keyManagementService = keyManagementService;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // Get all Erasure requests
        public async Task<IEnumerable<GetAllErasureRequestResponse>> GetAll()
        {
            var requests = await GetRequestsAsync(e => true);

            return requests.Select(e => new GetAllErasureRequestResponse
            {
                ErasureRequestId = e.ErasureRequestId,
                AttributeKeyId = e.AttributeKeyId,
                ConsentId = e.ConsentId,
                ExecutedBy = e.ExecutedBy,
                StudentId = e.Consent.StudentId,
                AttributeName = e.Consent.DataAttribute.AttributeName,
                ExecutedByFullName = e.ExecutedByNavigation?.FullName ?? string.Empty,
                PurposeName = e.Consent.ConsentPurpose.PurposeName,
                StudentFullName = e.Consent.Student.Account.FullName,
                ExecutedAt = e.ExecutedAt,
                RequestedAt = e.RequestedAt,
                KeyStatus = e.AttributeKey.Status,
                Status = e.Status
            });
        }

        // Get all Erasure requests with pagination
        public async Task<IEnumerable<GetAllErasureRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var requests = await GetRequestsAsync(e => true);

            return requests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new GetAllErasureRequestWithPaginationResponse
                {
                    ErasureRequestId = e.ErasureRequestId,
                    AttributeKeyId = e.AttributeKeyId,
                    ConsentId = e.ConsentId,
                    ExecutedBy = e.ExecutedBy,
                    StudentId = e.Consent.StudentId,
                    AttributeName = e.Consent.DataAttribute.AttributeName,
                    ExecutedByFullName = e.ExecutedByNavigation?.FullName ?? string.Empty,
                    PurposeName = e.Consent.ConsentPurpose.PurposeName,
                    StudentFullName = e.Consent.Student.Account.FullName,
                    ExecutedAt = e.ExecutedAt,
                    RequestedAt = e.RequestedAt,
                    KeyStatus = e.AttributeKey.Status,
                    Status = e.Status
                });
        }

        // Get Erasure request by Id
        public async Task<GetErasureRequestByIdResponse> GetByIdAsync(Guid ErasureRequestId)
        {
            var request = (await GetRequestsAsync(e => e.ErasureRequestId == ErasureRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Erasure request not found.");

            return new GetErasureRequestByIdResponse
            {
                ErasureRequestId = request.ErasureRequestId,
                AttributeKeyId = request.AttributeKeyId,
                ConsentId = request.ConsentId,
                ExecutedBy = request.ExecutedBy,
                StudentId = request.Consent.StudentId,
                AttributeName = request.Consent.DataAttribute.AttributeName,
                ExecutedByFullName = request.ExecutedByNavigation?.FullName ?? string.Empty,
                PurposeName = request.Consent.ConsentPurpose.PurposeName,
                StudentFullName = request.Consent.Student.Account.FullName,
                ExecutedAt = request.ExecutedAt,
                RequestedAt = request.RequestedAt,
                KeyStatus = request.AttributeKey.Status,
                Status = request.Status
            };
        }

        // Search Erasure requests by consent, executing steward, student or status
        public async Task<List<SearchErasureRequestResponse>> SearchAsync(SearchErasureRequestRequest request)
        {
            var requests = await GetRequestsAsync(e =>
                (request.ConsentId == null || e.ConsentId == request.ConsentId) &&
                (request.ExecutedBy == null || e.ExecutedBy == request.ExecutedBy) &&
                (request.StudentId == null || e.Consent.StudentId == request.StudentId) &&
                (string.IsNullOrWhiteSpace(request.Status) || e.Status == request.Status));

            return requests.Select(e => new SearchErasureRequestResponse
            {
                ErasureRequestId = e.ErasureRequestId,
                AttributeKeyId = e.AttributeKeyId,
                ConsentId = e.ConsentId,
                ExecutedBy = e.ExecutedBy,
                StudentId = e.Consent.StudentId,
                AttributeName = e.Consent.DataAttribute.AttributeName,
                ExecutedByFullName = e.ExecutedByNavigation?.FullName ?? string.Empty,
                PurposeName = e.Consent.ConsentPurpose.PurposeName,
                StudentFullName = e.Consent.Student.Account.FullName,
                ExecutedAt = e.ExecutedAt,
                RequestedAt = e.RequestedAt,
                KeyStatus = e.AttributeKey.Status,
                Status = e.Status
            }).ToList();
        }

        // Erasure first removes live payloads in SQL, then destroys the external key before final confirmation.
        public async Task<bool> ExecuteAsync(Guid StewardId, Guid ErasureRequestId)
        {
            var request = (await GetRequestsAsync(e => e.ErasureRequestId == ErasureRequestId)).FirstOrDefault()
                ?? throw new NotFoundException("Erasure request not found.");

            if (request.Status == "pending")
            {
                if (request.Consent.Status != "withdrawn")
                    throw new ConflictException("The consent behind this request is no longer withdrawn, so the erasure cannot go ahead.");
                if (await _consentRepository.ExistsAsync(c =>
                        c.StudentId == request.Consent.StudentId &&
                        c.DataAttributeId == request.Consent.DataAttributeId &&
                        c.Status == "granted"))
                    throw new ConflictException("The student has shared this attribute again under another purpose; erasing the key would wipe data that is still shared.");

                await _keyManagementService.EnsureKeyCanBeDestroyedAsync(request.AttributeKey.KeyManagementKeyId);

                var execution = await _erasureWorkflowRepository.ExecuteErasureAsync(ErasureRequestId, StewardId);
                if (execution.AttributeKeyId != request.AttributeKeyId ||
                    !string.Equals(
                        execution.KeyManagementKeyId,
                        request.AttributeKey.KeyManagementKeyId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The erasure procedure returned a different key reference than the loaded request.");
                }
            }
            else if (request.Status != "pendingKeyDestruction")
            {
                throw new ConflictException("This erasure has already been executed.");
            }

            await _keyManagementService.DestroyKeyAsync(request.AttributeKey.KeyManagementKeyId);
            await _erasureWorkflowRepository.ConfirmKmsKeyDestructionAsync(
                ErasureRequestId,
                request.AttributeKey.KeyManagementKeyId,
                StewardId);

            await _notificationService.NotifyAsync(request.Consent.StudentId, "Your data has been erased",
                $"Your {request.Consent.DataAttribute.AttributeName} values can no longer be read. The record that they existed and were withdrawn is kept for audit.", "/consents");
            await _activityLogService.LogCurrentAccountAsync("erasureExecuted",
                $"Erasure completed for {request.Consent.Student.Account.FullName}'s {request.Consent.DataAttribute.AttributeName} after Vault key destruction.");
            return true;
        }

        // Helper: load Erasure requests with the key, the consent (purpose, attribute, student) and the executing steward, newest first
        private async Task<List<Erasurerequest>> GetRequestsAsync(Expression<Func<Erasurerequest, bool>> predicate)
        {
            return await _erasureRequestRepository.SearchAsyncIncludeOrderBy(predicate, e => e.RequestedAt, false,
                e => e.AttributeKey, e => e.Consent.ConsentPurpose, e => e.Consent.DataAttribute, e => e.Consent.Student.Account, e => e.ExecutedByNavigation);
        }
    }
}
