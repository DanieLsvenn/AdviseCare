using AdviseCare.Application.Contracts.Requests.Referral;
using AdviseCare.Application.Contracts.Responses.Referral;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IReferralService
    {
        Task<ReferralResponse> CreateAsync(Guid fromAccountId, CreateReferralRequest request);
        Task<List<ReferralResponse>> GetIncomingAsync(Guid accountId, SearchMyReferralsRequest request);
        Task<List<ReferralResponse>> GetOutgoingAsync(Guid accountId, SearchMyReferralsRequest request);
        Task<List<ReferralResponse>> SearchAsync(Guid callerId, SearchReferralRequest request);
        Task<ReferralResponse> GetByIdAsync(Guid callerId, Guid referralId);
        Task<ReferralResponse> AcceptAsync(Guid receiverId, Guid referralId);
        Task<ReferralResponse> ReturnAsync(Guid receiverId, Guid referralId, ReturnReferralRequest request);
    }
}
