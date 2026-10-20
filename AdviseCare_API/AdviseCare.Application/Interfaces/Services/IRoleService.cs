using AdviseCare.Application.Contracts.Requests.Role;
using AdviseCare.Application.Contracts.Responses.Role;

namespace AdviseCare.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<GetAllRoleResponse>> GetAll();
        Task<IEnumerable<GetAllRoleWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize);
        Task<GetRoleByIdResponse> GetByIdAsync(Guid RoleId);
        Task<List<SearchRoleResponse>> SearchAsync(SearchRoleRequest request);
        Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request);
        Task<UpdateRoleResponse> UpdateAsync(Guid RoleId, UpdateRoleRequest request);
    }
}
