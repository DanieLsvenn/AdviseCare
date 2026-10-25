using AdviseCare.Application.Contracts.Requests.ConsentPurpose;
using AdviseCare.Application.Contracts.Responses.ConsentPurpose;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IConsentPurposeService
    {
        Task<IEnumerable<GetAllConsentPurposeResponse>> GetAll();
        Task<IEnumerable<GetAllConsentPurposeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetConsentPurposeByIdResponse> GetByIdAsync(Guid ConsentPurposeId);
        Task<List<SearchConsentPurposeResponse>> SearchAsync(SearchConsentPurposeRequest request);
        Task<CreateConsentPurposeResponse> CreateAsync(Guid CreatorId, CreateConsentPurposeRequest request);
        Task<UpdateConsentPurposeResponse> UpdateAsync(Guid ConsentPurposeId, UpdateConsentPurposeRequest request);
        Task<bool> ActivateAsync(Guid ConsentPurposeId);
        Task<bool> DeactivateAsync(Guid ConsentPurposeId);
        Task<bool> RetireAsync(Guid ConsentPurposeId);
        Task<IEnumerable<GetAllPurposeAttributeResponse>> GetAllPurposeAttributeAsync();
        Task<IEnumerable<GetAllPurposeAttributeWithPaginationResponse>> GetAllPurposeAttributeWithPaginationAsync(int page, int pageSize);
        Task<GetPurposeAttributeByIdResponse> GetPurposeAttributeByIdAsync(Guid ConsentPurposeId, Guid DataAttributeId);
        Task<List<SearchPurposeAttributeResponse>> SearchPurposeAttributeAsync(SearchPurposeAttributeRequest request);
        Task<CreatePurposeAttributeResponse> CreatePurposeAttributeAsync(CreatePurposeAttributeRequest request);
        Task<bool> DeletePurposeAttributeAsync(Guid ConsentPurposeId, Guid DataAttributeId);
    }
}
