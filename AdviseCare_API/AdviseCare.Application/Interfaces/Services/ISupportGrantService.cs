using AdviseCare.Application.Contracts.Requests.SupportGrant;
using AdviseCare.Application.Contracts.Responses.SupportGrant;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISupportGrantService
    {
        Task<IEnumerable<GetAllSupportGrantResponse>> GetAll();
        Task<IEnumerable<GetAllSupportGrantWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetSupportGrantByIdResponse> GetByIdAsync(Guid SupportGrantId);
        Task<List<SearchSupportGrantResponse>> SearchAsync(SearchSupportGrantRequest request);
        Task<CreateSupportGrantResponse> CreateAsync(Guid CreatorId, CreateSupportGrantRequest request);
        Task<bool> RevokeAsync(Guid OfficerId, Guid SupportGrantId);
    }
}
