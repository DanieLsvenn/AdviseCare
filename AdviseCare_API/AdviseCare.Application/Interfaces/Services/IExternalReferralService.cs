using AdviseCare.Application.Contracts.Requests.ExternalReferral;
using AdviseCare.Application.Contracts.Responses.ExternalReferral;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IExternalReferralService
    {
        Task<IEnumerable<GetAllExternalReferralResponse>> GetAll();
        Task<IEnumerable<GetAllExternalReferralWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetExternalReferralByIdResponse> GetByIdAsync(Guid ExternalReferralId);
        Task<List<SearchExternalReferralResponse>> SearchAsync(SearchExternalReferralRequest request);
        Task<CreateExternalReferralResponse> CreateAsync(Guid CreatorId, CreateExternalReferralRequest request);
        Task<UpdateExternalReferralResponse> UpdateAsync(Guid ExternalReferralId, UpdateExternalReferralRequest request);
        Task<CloseExternalReferralResponse> CloseAsync(Guid ExternalReferralId);
    }
}
