using AdviseCare.Application.Contracts.Requests.Role;
using AdviseCare.Application.Contracts.Responses.Role;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;

namespace AdviseCare.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IActivityLogService _activityLogService;

        public RoleService(
            IGenericRepository<Role> roleRepository,
            IActivityLogService activityLogService)
        {
            _roleRepository = roleRepository;
            _activityLogService = activityLogService;
        }

        // Get all Roles
        public async Task<IEnumerable<GetAllRoleResponse>> GetAll()
        {
            var roles = await _roleRepository.GetAllAsyncOrderBy(r => r.RoleName);

            return roles.Select(r => new GetAllRoleResponse
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName
            });
        }

        // Get all Roles with pagination
        public async Task<IEnumerable<GetAllRoleWithPaginationResponse>> GetAllWithPaginationAsync(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Page and page size must be at least 1.");

            var roles = await _roleRepository.GetAllAsyncOrderBy(r => r.RoleName);

            return roles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new GetAllRoleWithPaginationResponse
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                });
        }

        // Get Role by Id
        public async Task<GetRoleByIdResponse> GetByIdAsync(Guid RoleId)
        {
            var role = await _roleRepository.GetByIdAsync(RoleId)
                ?? throw new NotFoundException("Role not found.");

            return new GetRoleByIdResponse
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };
        }

        // Search Roles by role name
        public async Task<List<SearchRoleResponse>> SearchAsync(SearchRoleRequest request)
        {
            var roles = await _roleRepository.SearchAsyncOrderBy(r =>
                (string.IsNullOrWhiteSpace(request.RoleName) || r.RoleName.Contains(request.RoleName)),
                r => r.RoleName);

            return roles.Select(r => new SearchRoleResponse
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName
            }).ToList();
        }

        // Create Role
        public async Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request)
        {
            if (await _roleRepository.ExistsAsync(r => r.RoleName == request.RoleName.Trim()))
                throw new ConflictException($"A role with name '{request.RoleName}' already exists.");

            var role = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = request.RoleName.Trim()
            };
            await _roleRepository.CreateAsync(role);

            await _activityLogService.LogCurrentAccountAsync("roleCreated", $"Created role {role.RoleName}");

            return new CreateRoleResponse
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };
        }

        // Update Role
        public async Task<UpdateRoleResponse> UpdateAsync(Guid RoleId, UpdateRoleRequest request)
        {
            var role = await _roleRepository.GetByIdAsync(RoleId)
                ?? throw new NotFoundException("Role not found.");

            if (await _roleRepository.ExistsAsync(r => r.RoleName == request.RoleName.Trim() && r.RoleId != RoleId))
                throw new ConflictException($"A role with name '{request.RoleName}' already exists.");

            role.RoleName = request.RoleName.Trim();
            await _roleRepository.UpdateAsync(role);

            await _activityLogService.LogCurrentAccountAsync("roleUpdated", $"Renamed role to {role.RoleName}");

            return new UpdateRoleResponse
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };
        }
    }
}
