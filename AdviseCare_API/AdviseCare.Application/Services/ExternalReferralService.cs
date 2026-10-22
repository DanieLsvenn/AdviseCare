using AdviseCare.Application.Contracts.Requests.ExternalReferral;
using AdviseCare.Application.Contracts.Responses.ExternalReferral;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class ExternalReferralService(
        IGenericRepository<Externalreferral> externalReferralRepository,
        IGenericRepository<Externalservice> externalServiceRepository,
        IGenericRepository<Case> caseRepository) : IExternalReferralService
    {
        private readonly IGenericRepository<Externalreferral> _externalReferralRepository = externalReferralRepository;
        private readonly IGenericRepository<Externalservice> _externalServiceRepository = externalServiceRepository;
        private readonly IGenericRepository<Case> _caseRepository = caseRepository;

        public async Task<ExternalReferralResponse> CreateAsync(Guid referredBy, CreateExternalReferralRequest request)
        {
            if (!await _caseRepository.ExistsAsync(caseRecord => caseRecord.CaseId == request.CaseId))
                throw new NotFoundException("Case not found.");

            var externalService = await _externalServiceRepository.GetByIdAsync(request.ExternalServiceId)
                ?? throw new NotFoundException("External service not found.");

            if (!externalService.IsActive)
                throw new ValidationException("New referrals can only be made to active external services.");

            var referral = new Externalreferral
            {
                ExternalReferralId = Guid.NewGuid(),
                CaseId = request.CaseId,
                ExternalServiceId = request.ExternalServiceId,
                ReferredBy = referredBy,
                FollowUpDueAt = request.FollowUpDueAt?.UtcDateTime,
                ReferredAt = DateTime.UtcNow,
                IsOverdue = false,
                Status = "sent"
            };

            await _externalReferralRepository.CreateAsync(referral);
            return ToResponse(referral, externalService, DateTime.UtcNow);
        }

        public async Task<List<ExternalReferralResponse>> SearchAsync(SearchExternalReferralRequest request)
        {
            var now = DateTime.UtcNow;
            var referrals = await _externalReferralRepository.SearchAsyncIncludeOrderBy(
                referral =>
                    (!request.CaseId.HasValue || referral.CaseId == request.CaseId.Value) &&
                    (!request.ExternalServiceId.HasValue || referral.ExternalServiceId == request.ExternalServiceId.Value) &&
                    (string.IsNullOrWhiteSpace(request.Status) || referral.Status == request.Status.Trim()) &&
                    (!request.IsOverdue.HasValue ||
                        (request.IsOverdue.Value
                            ? referral.IsOverdue || (referral.Status == "sent" && referral.FollowUpDueAt.HasValue && referral.FollowUpDueAt <= now)
                            : !referral.IsOverdue && !(referral.Status == "sent" && referral.FollowUpDueAt.HasValue && referral.FollowUpDueAt <= now))),
                referral => referral.ReferredAt,
                false,
                referral => referral.ExternalService);

            return referrals.Select(referral => ToResponse(referral, referral.ExternalService, now)).ToList();
        }

        public async Task<ExternalReferralResponse> RecordFollowUpAsync(
            Guid externalReferralId,
            RecordExternalReferralFollowUpRequest request)
        {
            var referral = await _externalReferralRepository.GetByIdAsyncInclude(
                externalReferralId,
                item => item.ExternalService)
                ?? throw new NotFoundException("External referral not found.");

            if (referral.Status != "sent")
                throw new ConflictException("Only referrals with sent status can receive a follow-up.");

            var outcome = request.FollowUpOutcome.Trim();
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ValidationException("Follow-up outcome is required.");

            referral.FollowUpOutcome = outcome;
            referral.IsOverdue = false;
            referral.Status = "followedUp";
            await _externalReferralRepository.UpdateAsync(referral);

            return ToResponse(referral, referral.ExternalService, DateTime.UtcNow);
        }

        private static ExternalReferralResponse ToResponse(
            Externalreferral referral,
            Externalservice externalService,
            DateTime now)
        {
            var isOverdue = referral.IsOverdue ||
                (referral.Status == "sent" && referral.FollowUpDueAt.HasValue && referral.FollowUpDueAt.Value <= now);

            return new ExternalReferralResponse
            {
                ExternalReferralId = referral.ExternalReferralId,
                CaseId = referral.CaseId,
                ExternalServiceId = referral.ExternalServiceId,
                ExternalServiceName = externalService.ServiceName,
                ReferredBy = referral.ReferredBy,
                FollowUpDueAt = referral.FollowUpDueAt.HasValue
                    ? DateTime.SpecifyKind(referral.FollowUpDueAt.Value, DateTimeKind.Utc)
                    : null,
                FollowUpOutcome = referral.FollowUpOutcome ?? string.Empty,
                ReferredAt = DateTime.SpecifyKind(referral.ReferredAt, DateTimeKind.Utc),
                IsOverdue = isOverdue,
                Status = referral.Status
            };
        }
    }
}
