using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AdviseCare.Application.Contracts.Requests.Account
{
    public class SearchAccountRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public Guid? FacultyId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateAccountRequest
    {
        [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Email is not a valid email address."), MaxLength(255,
            ErrorMessage = "Email must be under 255 characters.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full name is required."), MaxLength(255,
            ErrorMessage = "Full name must be under 255 characters.")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Username is required."), MaxLength(255,
            ErrorMessage = "Username must be under 255 characters.")]
        public string UserName { get; set; } = string.Empty;
        public Guid? FacultyId { get; set; }
        [Required(ErrorMessage = "At least one role is required."), MinLength(1,
            ErrorMessage = "At least one role is required.")]
        public List<string> RoleNames { get; set; } = new();
        // Student accounts only (required when RoleNames contains Student, must stay empty otherwise)
        [MaxLength(50, ErrorMessage = "Student code must be under 50 characters.")]
        public string StudentCode { get; set; } = string.Empty;
        [RegularExpression("^(enrolled|onLeave|withdrawn|graduated)?$",
            ErrorMessage = "Academic status must be enrolled, onLeave, withdrawn or graduated.")]
        public string AcademicStatus { get; set; } = string.Empty;
    }

    public class UpdateAccountRequest
    {
        [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Email is not a valid email address."), MaxLength(255,
            ErrorMessage = "Email must be under 255 characters.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Full name is required."), MaxLength(255,
            ErrorMessage = "Full name must be under 255 characters.")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Username is required."), MaxLength(255,
            ErrorMessage = "Username must be under 255 characters.")]
        public string UserName { get; set; } = string.Empty;
        public Guid? FacultyId { get; set; }
    }

    public class ImportAccountRequest
    {
        [Required(ErrorMessage = "An Excel file (.xlsx) is required.")]
        public IFormFile? File { get; set; }
    }

    public class SendLinkingEmailRequest
    {
        [Required(ErrorMessage = "At least one account is required."), MinLength(1,
            ErrorMessage = "At least one account is required.")]
        public List<Guid> AccountIds { get; set; } = new();
    }

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

    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID token is required.")]
        public string IdToken { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
    }

    public class LinkGoogleAccountRequest
    {
        [Required(ErrorMessage = "Linking token is required.")]
        public string Token { get; set; } = string.Empty;
        [Required(ErrorMessage = "Google ID token is required.")]
        public string IdToken { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Reset token is required.")]
        public string Token { get; set; } = string.Empty;
        [Required(ErrorMessage = "New password is required."), MinLength(6,
            ErrorMessage = "New password must contain at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Confirm password is required."), Compare(nameof(NewPassword),
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "New password is required."), MinLength(6,
            ErrorMessage = "New password must contain at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Confirm password is required."), Compare(nameof(NewPassword),
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateMyProfileRequest
    {
        [Required(ErrorMessage = "Username is required."), MaxLength(255,
            ErrorMessage = "Username must be under 255 characters.")]
        public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Preferred language is required."), RegularExpression("^(vi|en)$",
            ErrorMessage = "Preferred language must be vi or en.")]
        public string PreferredLanguage { get; set; } = "vi";
    }

    public class UploadAvatarRequest
    {
        [Required(ErrorMessage = "An image file is required.")]
        public IFormFile? File { get; set; }
    }

    public class SearchAccountRoleRequest
    {
        public Guid? AccountId { get; set; }
        public Guid? RoleId { get; set; }
    }

    public class CreateAccountRoleRequest
    {
        [Required(ErrorMessage = "Account Id is required.")]
        public Guid AccountId { get; set; } = Guid.Empty;
        [Required(ErrorMessage = "Role Id is required.")]
        public Guid RoleId { get; set; } = Guid.Empty;
        // Only when giving the Student role to an account that has no student profile yet
        [MaxLength(50, ErrorMessage = "Student code must be under 50 characters.")]
        public string StudentCode { get; set; } = string.Empty;
        [RegularExpression("^(enrolled|onLeave|withdrawn|graduated)?$",
            ErrorMessage = "Academic status must be enrolled, onLeave, withdrawn or graduated.")]
        public string AcademicStatus { get; set; } = string.Empty;
    }

    public class UpdateStudentProfileRequest
    {
        [Required(ErrorMessage = "Student code is required."), MaxLength(50,
            ErrorMessage = "Student code must be under 50 characters.")]
        public string StudentCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "Academic status is required."), RegularExpression("^(enrolled|onLeave|withdrawn|graduated)$",
            ErrorMessage = "Academic status must be enrolled, onLeave, withdrawn or graduated.")]
        public string AcademicStatus { get; set; } = "enrolled";
    }
}
