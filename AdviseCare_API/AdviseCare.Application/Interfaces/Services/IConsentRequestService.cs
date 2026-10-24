using AdviseCare.Application.Contracts.Requests.ConsentRequest;
using AdviseCare.Application.Contracts.Responses.ConsentRequest;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IConsentRequestService
    {
        Task<IEnumerable<GetAllConsentRequestResponse>> GetAll();
        Task<IEnumerable<GetAllConsentRequestWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetConsentRequestByIdResponse> GetByIdAsync(Guid ConsentRequestId);
        Task<List<SearchConsentRequestResponse>> SearchAsync(SearchConsentRequestRequest request);
        Task<CreateConsentRequestResponse> CreateAsync(Guid CreatorId, CreateConsentRequestRequest request);
        Task<UpdateConsentRequestResponse> UpdateAsync(Guid AdvisorId, Guid ConsentRequestId, UpdateConsentRequestRequest request);
        Task<bool> GrantAsync(Guid StudentId, Guid ConsentRequestId);
        Task<bool> DenyAsync(Guid StudentId, Guid ConsentRequestId);
        Task<bool> WithdrawAsync(Guid AdvisorId, Guid ConsentRequestId);
        Task<List<GetMyConsentRequestResponse>> GetMyReceivedRequestsAsync(Guid StudentId);
        Task<List<GetMyConsentRequestResponse>> GetMySentRequestsAsync(Guid AdvisorId);
    }
}
