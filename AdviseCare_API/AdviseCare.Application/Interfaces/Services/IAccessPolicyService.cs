using AdviseCare.Application.Contracts.Requests.AccessPolicy;
using AdviseCare.Application.Contracts.Responses.AccessPolicy;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAccessPolicyService
    {
        Task<IEnumerable<GetAllAccessPolicyResponse>> GetAll();
        Task<IEnumerable<GetAllAccessPolicyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetAccessPolicyByIdResponse> GetByIdAsync(Guid AccessPolicyId);
        Task<List<SearchAccessPolicyResponse>> SearchAsync(SearchAccessPolicyRequest request);
        Task<CreateAccessPolicyResponse> CreateAsync(Guid CreatorId, CreateAccessPolicyRequest request);
        Task<UpdateAccessPolicyResponse> UpdateAsync(Guid AccessPolicyId, UpdateAccessPolicyRequest request);
        Task<bool> ActivateAsync(Guid AccessPolicyId);
        Task<bool> RevokeAsync(Guid AccessPolicyId);
        Task<TestAccessPolicyResponse> TestAsync(Guid AccessPolicyId, TestAccessPolicyRequest request);
    }
}
