using AdviseCare.Application.Contracts.Requests.SupportCategory;
using AdviseCare.Application.Contracts.Responses.SupportCategory;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISupportCategoryService
    {
        Task<IEnumerable<GetAllSupportCategoryResponse>> GetAll();
        Task<IEnumerable<GetAllSupportCategoryWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetSupportCategoryByIdResponse> GetByIdAsync(Guid SupportCategoryId);
        Task<List<SearchSupportCategoryResponse>> SearchAsync(SearchSupportCategoryRequest request);
        Task<CreateSupportCategoryResponse> CreateAsync(Guid CreatorId, CreateSupportCategoryRequest request);
        Task<UpdateSupportCategoryResponse> UpdateAsync(Guid SupportCategoryId, UpdateSupportCategoryRequest request);
        Task<GetSupportCategoryShareableAttributesResponse> GetShareableAttributesAsync(Guid AccountId, bool IsStudent, Guid SupportCategoryId);
    }
}
