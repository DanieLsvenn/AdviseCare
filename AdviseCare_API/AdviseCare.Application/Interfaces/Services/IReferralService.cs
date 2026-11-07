using AdviseCare.Application.Contracts.Requests.Referral;
using AdviseCare.Application.Contracts.Responses.Referral;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IReferralService
    {
        Task<IEnumerable<GetAllReferralResponse>> GetAll();
        Task<IEnumerable<GetAllReferralWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetReferralByIdResponse> GetByIdAsync(Guid CallerId, Guid ReferralId);
        Task<List<SearchReferralResponse>> SearchAsync(Guid CallerId, SearchReferralRequest request);
        Task<CreateReferralResponse> CreateAsync(Guid CreatorId, CreateReferralRequest request);
        Task<List<GetMyIncomingReferralResponse>> GetMyIncomingAsync(Guid AccountId, SearchMyReferralRequest request);
        Task<List<GetMyOutgoingReferralResponse>> GetMyOutgoingAsync(Guid AccountId, SearchMyReferralRequest request);
        Task<AcceptReferralResponse> AcceptAsync(Guid ReceiverId, Guid ReferralId);
        Task<ReturnReferralResponse> ReturnAsync(Guid ReceiverId, Guid ReferralId, ReturnReferralRequest request);
    }
}
