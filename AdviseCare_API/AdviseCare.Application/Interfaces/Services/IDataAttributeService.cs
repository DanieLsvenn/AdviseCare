using AdviseCare.Application.Contracts.Requests.DataAttribute;
using AdviseCare.Application.Contracts.Responses.DataAttribute;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IDataAttributeService
    {
        Task<IEnumerable<GetAllDataAttributeResponse>> GetAll();
        Task<IEnumerable<GetAllDataAttributeWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetDataAttributeByIdResponse> GetByIdAsync(Guid DataAttributeId);
        Task<List<SearchDataAttributeResponse>> SearchAsync(SearchDataAttributeRequest request);
        Task<CreateDataAttributeResponse> CreateAsync(CreateDataAttributeRequest request);
        Task<UpdateDataAttributeResponse> UpdateAsync(Guid DataAttributeId, UpdateDataAttributeRequest request);
        Task<bool> ArchiveAsync(Guid DataAttributeId);
    }
}
