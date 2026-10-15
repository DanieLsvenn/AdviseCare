using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email or username is required."), MaxLength(255,
            ErrorMessage = "Email or username must be under 255 characters.")]
        public string Identifier { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required."), MinLength(6,
            ErrorMessage = "Password must contain at least 6 characters.")]
        public string Password { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
    }
}
