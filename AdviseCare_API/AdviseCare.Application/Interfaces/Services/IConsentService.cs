using AdviseCare.Application.Contracts.Requests.Consent;
using AdviseCare.Application.Contracts.Responses.Consent;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IConsentService
    {
        Task<IEnumerable<GetAllConsentResponse>> GetAll();
        Task<IEnumerable<GetAllConsentWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetConsentByIdResponse> GetByIdAsync(Guid ConsentId);
        Task<List<SearchConsentResponse>> SearchAsync(SearchConsentRequest request);
        Task<List<CreateConsentResponse>> CreateAsync(Guid StudentId, CreateConsentRequest request);
        Task<UpdateConsentResponse> UpdateAsync(Guid StudentId, Guid ConsentId, UpdateConsentRequest request);
        Task<bool> WithdrawAsync(Guid StudentId, Guid ConsentId);
        Task<int> WithdrawPurposeAsync(Guid StudentId, Guid ConsentPurposeId);
        Task<List<GetMyConsentResponse>> GetMyConsentsAsync(Guid StudentId);
        Task<IEnumerable<GetAllConsentHistoryResponse>> GetAllConsentHistoryAsync();
        Task<IEnumerable<GetAllConsentHistoryWithPaginationResponse>> GetAllConsentHistoryWithPaginationAsync(int page, int pageSize);
        Task<GetConsentHistoryByIdResponse> GetConsentHistoryByIdAsync(Guid ConsentHistoryId);
        Task<List<SearchConsentHistoryResponse>> SearchConsentHistoryAsync(SearchConsentHistoryRequest request);
        Task<List<GetMyConsentHistoryResponse>> GetMyConsentHistoryAsync(Guid StudentId);
    }
}
