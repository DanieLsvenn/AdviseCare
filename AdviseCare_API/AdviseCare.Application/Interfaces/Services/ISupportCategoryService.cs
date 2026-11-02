using AdviseCare.Application.Contracts.Requests.SupportCategory;
using AdviseCare.Application.Contracts.Responses.SupportCategory;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface ISupportCategoryService
    {
        Task<List<SupportCategoryResponse>> GetAllAsync(int? page, int? pageSize);
        Task<List<SupportCategoryResponse>> SearchAsync(SearchSupportCategoryRequest request);
        Task<SupportCategoryResponse> GetByIdAsync(Guid supportCategoryId);
        Task<ShareableAttributesResponse> GetShareableAttributesAsync(Guid supportCategoryId, Guid accountId, bool isStudent);
        Task<SupportCategoryResponse> CreateAsync(Guid createdBy, CreateSupportCategoryRequest request);
        Task<SupportCategoryResponse> UpdateAsync(Guid supportCategoryId, UpdateSupportCategoryRequest request);
    }
}
