namespace AdviseCare.Application.Contracts.Responses.Role
{
    public class GetAllRoleResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class GetAllRoleWithPaginationResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class GetRoleByIdResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class SearchRoleResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateRoleResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateRoleResponse
    {
        public Guid RoleId { get; set; } = Guid.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
