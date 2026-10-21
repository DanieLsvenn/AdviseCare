using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Role
{
    public class SearchRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateRoleRequest
    {
        [Required(ErrorMessage = "Role name is required."), MaxLength(255,
            ErrorMessage = "Role name must be under 255 characters.")]
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateRoleRequest
    {
        [Required(ErrorMessage = "Role name is required."), MaxLength(255,
            ErrorMessage = "Role name must be under 255 characters.")]
        public string RoleName { get; set; } = string.Empty;
    }
}
