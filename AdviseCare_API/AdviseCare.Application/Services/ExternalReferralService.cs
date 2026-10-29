using AdviseCare.Application.Contracts.Requests.ExternalReferral;
using AdviseCare.Application.Contracts.Responses.ExternalReferral;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using System.Linq.Expressions;

namespace AdviseCare.Application.Services
{
    // External referrals: the Welfare Officer sends a referred case to an outside service and follows it up (sent -> followedUp -> closed);
    // a referral past its follow-up date without an outcome is overdue (also marked by the background check)
    public class ExternalReferralService : IExternalReferralService
    {
        private readonly IGenericRepository<Externalreferral> _externalReferralRepository;
        private readonly IGenericRepository<Externalservice> _externalServiceRepository;
        private readonly IGenericRepository<Case> _caseRepository;
        private readonly IGenericRepository<Referral> _referralRepository;
        private readonly IActivityLogService _activityLogService;

        public ExternalReferralService(
            IGenericRepository<Externalreferral> externalReferralRepository,
            IGenericRepository<Externalservice> externalServiceRepository,
            IGenericRepository<Case> caseRepository,
            IGenericRepository<Referral> referralRepository,
            IActivityLogService activityLogService)
        {
            _externalReferralRepository = externalReferralRepository;
            _externalServiceRepository = externalServiceRepository;
            _caseRepository = caseRepository;
            _referralRepository = referralRepository;
            _activityLogService = activityLogService;
        }

        // Get all External referrals
        public async Task<IEnumerable<GetAllExternalReferralResponse>> GetAll()
        {
            var now = DateTime.UtcNow;
            var referrals = await GetReferralsAsync(r => true);

            return referrals.Select(r => new GetAllExternalReferralResponse
            {
                ExternalReferralId = r.ExternalReferralId,
                CaseId = r.CaseId,
                ExternalServiceId = r.ExternalServiceId,
                ReferredBy = r.ReferredBy,
                ExternalServiceName = r.ExternalService.ServiceName,
                FollowUpOutcome = r.FollowUpOutcome ?? string.Empty,
                ReferredByFullName = r.ReferredByNavigation.FullName,
                FollowUpDueAt = r.FollowUpDueAt,
                ReferredAt = r.ReferredAt,
                IsOverdue = r.IsOverdue || (r.Status == "sent" && r.FollowUpDueAt <= now),
                Status = r.Status
            });
        }

        // Get all External referrals with pagination
        public async Task<IEnumerable<GetAllExternalReferralWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var now = DateTime.UtcNow;
            var referrals = await GetReferralsAsync(r => true);

            return referrals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllExternalReferralWithPaginationResponse
                {
                    ExternalReferralId = r.ExternalReferralId,
                    CaseId = r.CaseId,
                    ExternalServiceId = r.ExternalServiceId,
                    ReferredBy = r.ReferredBy,
                    ExternalServiceName = r.ExternalService.ServiceName,
                    FollowUpOutcome = r.FollowUpOutcome ?? string.Empty,
                    ReferredByFullName = r.ReferredByNavigation.FullName,
                    FollowUpDueAt = r.FollowUpDueAt,
                    ReferredAt = r.ReferredAt,
                    IsOverdue = r.IsOverdue || (r.Status == "sent" && r.FollowUpDueAt <= now),
                    Status = r.Status
                });
        }

        // Get External referral by Id
        public async Task<GetExternalReferralByIdResponse> GetByIdAsync(Guid ExternalReferralId)
        {
            var referral = (await GetReferralsAsync(r => r.ExternalReferralId == ExternalReferralId)).FirstOrDefault()
                ?? throw new NotFoundException("External referral not found.");

            return new GetExternalReferralByIdResponse
            {
                ExternalReferralId = referral.ExternalReferralId,
                CaseId = referral.CaseId,
                ExternalServiceId = referral.ExternalServiceId,
                ReferredBy = referral.ReferredBy,
                ExternalServiceName = referral.ExternalService.ServiceName,
                FollowUpOutcome = referral.FollowUpOutcome ?? string.Empty,
                ReferredByFullName = referral.ReferredByNavigation.FullName,
                FollowUpDueAt = referral.FollowUpDueAt,
                ReferredAt = referral.ReferredAt,
                IsOverdue = referral.IsOverdue || (referral.Status == "sent" && referral.FollowUpDueAt <= DateTime.UtcNow),
                Status = referral.Status
            };
        }

        // Search External referrals by case, service, overdue state or status
        public async Task<List<SearchExternalReferralResponse>> SearchAsync(SearchExternalReferralRequest request)
        {
            var now = DateTime.UtcNow;
            var referrals = await GetReferralsAsync(r =>
                (request.CaseId == null || r.CaseId == request.CaseId) &&
                (request.ExternalServiceId == null || r.ExternalServiceId == request.ExternalServiceId) &&
                (string.IsNullOrWhiteSpace(request.Status) || r.Status == request.Status) &&
                (request.IsOverdue == null || (r.IsOverdue || (r.Status == "sent" && r.FollowUpDueAt <= now)) == request.IsOverdue));

            return referrals.Select(r => new SearchExternalReferralResponse
            {
                ExternalReferralId = r.ExternalReferralId,
                CaseId = r.CaseId,
                ExternalServiceId = r.ExternalServiceId,
                ReferredBy = r.ReferredBy,
                ExternalServiceName = r.ExternalService.ServiceName,
                FollowUpOutcome = r.FollowUpOutcome ?? string.Empty,
                ReferredByFullName = r.ReferredByNavigation.FullName,
                FollowUpDueAt = r.FollowUpDueAt,
                ReferredAt = r.ReferredAt,
                IsOverdue = r.IsOverdue || (r.Status == "sent" && r.FollowUpDueAt <= now),
                Status = r.Status
            }).ToList();
        }

        // Create External referral: the case must be open and referred to (and accepted by) this Welfare Officer, the service must be active
        public async Task<CreateExternalReferralResponse> CreateAsync(Guid CreatorId, CreateExternalReferralRequest request)
        {
            await EnsureWelfareCaseAsync(CreatorId, request.CaseId);
            var service = await _externalServiceRepository.GetByIdAsync(request.ExternalServiceId)
                ?? throw new NotFoundException("External service not found.");
            if (!service.IsActive)
                throw new ConflictException("New referrals can only be sent to an active external service.");

            var referral = new Externalreferral
            {
                ExternalReferralId = Guid.NewGuid(),
                CaseId = request.CaseId,
                ExternalServiceId = request.ExternalServiceId,
                ReferredBy = CreatorId,
                FollowUpDueAt = request.FollowUpDueAt?.ToUniversalTime(),
                ReferredAt = DateTime.UtcNow,
                IsOverdue = false,
                Status = "sent"
            };
            await _externalReferralRepository.CreateAsync(referral);

            await _activityLogService.LogAsync(CreatorId, "externalReferralCreated", $"Referred case {referral.CaseId} to {service.ServiceName}");

            var created = (await GetReferralsAsync(r => r.ExternalReferralId == referral.ExternalReferralId)).Single();
            return new CreateExternalReferralResponse
            {
                ExternalReferralId = created.ExternalReferralId,
                CaseId = created.CaseId,
                ExternalServiceId = created.ExternalServiceId,
                ReferredBy = created.ReferredBy,
                ExternalServiceName = created.ExternalService.ServiceName,
                FollowUpOutcome = created.FollowUpOutcome ?? string.Empty,
                ReferredByFullName = created.ReferredByNavigation.FullName,
                FollowUpDueAt = created.FollowUpDueAt,
                ReferredAt = created.ReferredAt,
                IsOverdue = false,
                Status = created.Status
            };
        }

        // Update External referral: record the follow-up outcome of a sent referral (sent -> followedUp)
        public async Task<UpdateExternalReferralResponse> UpdateAsync(Guid ExternalReferralId, UpdateExternalReferralRequest request)
        {
            var referral = (await GetReferralsAsync(r => r.ExternalReferralId == ExternalReferralId)).FirstOrDefault()
                ?? throw new NotFoundException("External referral not found.");
            if (referral.Status != "sent")
                throw new ConflictException("Only a sent referral can receive a follow-up outcome.");

            referral.FollowUpOutcome = request.FollowUpOutcome.Trim();
            referral.IsOverdue = false;
            referral.Status = "followedUp";
            await _externalReferralRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("externalReferralUpdated", $"Recorded the follow-up of the referral of case {referral.CaseId} to {referral.ExternalService.ServiceName}");

            return new UpdateExternalReferralResponse
            {
                ExternalReferralId = referral.ExternalReferralId,
                CaseId = referral.CaseId,
                ExternalServiceId = referral.ExternalServiceId,
                ReferredBy = referral.ReferredBy,
                ExternalServiceName = referral.ExternalService.ServiceName,
                FollowUpOutcome = referral.FollowUpOutcome,
                ReferredByFullName = referral.ReferredByNavigation.FullName,
                FollowUpDueAt = referral.FollowUpDueAt,
                ReferredAt = referral.ReferredAt,
                IsOverdue = false,
                Status = referral.Status
            };
        }

        // Close External referral: ends a followed-up referral (followedUp -> closed)
        public async Task<CloseExternalReferralResponse> CloseAsync(Guid ExternalReferralId)
        {
            var referral = (await GetReferralsAsync(r => r.ExternalReferralId == ExternalReferralId)).FirstOrDefault()
                ?? throw new NotFoundException("External referral not found.");
            if (referral.Status != "followedUp")
                throw new ConflictException("Only a followed-up referral can be closed.");

            referral.Status = "closed";
            await _externalReferralRepository.SaveAsync();

            await _activityLogService.LogCurrentAccountAsync("externalReferralUpdated", $"Closed the referral of case {referral.CaseId} to {referral.ExternalService.ServiceName}");

            return new CloseExternalReferralResponse
            {
                ExternalReferralId = referral.ExternalReferralId,
                CaseId = referral.CaseId,
                ExternalServiceId = referral.ExternalServiceId,
                ReferredBy = referral.ReferredBy,
                ExternalServiceName = referral.ExternalService.ServiceName,
                FollowUpOutcome = referral.FollowUpOutcome ?? string.Empty,
                ReferredByFullName = referral.ReferredByNavigation.FullName,
                FollowUpDueAt = referral.FollowUpDueAt,
                ReferredAt = referral.ReferredAt,
                IsOverdue = false,
                Status = referral.Status
            };
        }

        // Helper: the Welfare Officer may only act on an open case that was referred to them and that they accepted (WF2 8.2)
        private async Task EnsureWelfareCaseAsync(Guid OfficerId, Guid CaseId)
        {
            var caseRecord = await _caseRepository.GetByIdAsync(CaseId)
                ?? throw new NotFoundException("Case not found.");
            if (caseRecord.Status == "closed")
                throw new ConflictException("The case is closed.");
            if (!await _referralRepository.ExistsAsync(r => r.CaseId == CaseId && r.ToAccountId == OfficerId && r.Status == "accepted"))
                throw new ForbiddenException("This case has not been referred to you (or you have not accepted the referral yet).");
        }

        // Helper: load External referrals with their service and referring officer, newest first
        private async Task<List<Externalreferral>> GetReferralsAsync(Expression<Func<Externalreferral, bool>> predicate)
        {
            return await _externalReferralRepository.SearchAsyncIncludeOrderBy(predicate, r => r.ReferredAt, false,
                r => r.ExternalService, r => r.ReferredByNavigation);
        }
    }
}
