using AdviseCare.Application.Contracts.Requests.ExternalReferral;
using AdviseCare.Application.Contracts.Responses.ExternalReferral;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IExternalReferralService
    {
        Task<ExternalReferralResponse> CreateAsync(Guid referredBy, CreateExternalReferralRequest request);
        Task<List<ExternalReferralResponse>> SearchAsync(SearchExternalReferralRequest request);
        Task<ExternalReferralResponse> RecordFollowUpAsync(Guid externalReferralId, RecordExternalReferralFollowUpRequest request);
    }
}
