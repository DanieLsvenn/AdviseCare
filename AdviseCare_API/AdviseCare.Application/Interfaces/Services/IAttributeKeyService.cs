using AdviseCare.Application.Contracts.Requests.AttributeKey;
using AdviseCare.Application.Contracts.Responses.AttributeKey;
using AdviseCare.Application.DTOs;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IAttributeKeyService
    {
        Task<IEnumerable<GetAllAttributeKeyResponse>> GetAll();
        Task<IEnumerable<GetAllAttributeKeyWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetAttributeKeyByIdResponse> GetByIdAsync(Guid AttributeKeyId);
        Task<List<SearchAttributeKeyResponse>> SearchAsync(SearchAttributeKeyRequest request);
        Task<List<string>> EncryptValuesAsync(List<PlainAttributeValue> values);
        Task<List<DecryptedAttributeValue>> DecryptValuesAsync(List<EncryptedAttributeValue> values);
    }
}
