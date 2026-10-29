namespace AdviseCare.Application.Contracts.Responses.Account
{
    public class GetAllAccountResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class GetAllAccountWithPaginationResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class GetAccountByIdResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class SearchAccountResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class CreateAccountResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateAccountResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class ImportAccountResponse
    {
        public bool IsSuccess { get; set; } = false;
        public int CreatedCount { get; set; } = 0;
        public List<Guid> CreatedAccountIds { get; set; } = new();
        public List<ImportAccountErrorResponse> Errors { get; set; } = new();
    }

    public class ImportAccountErrorResponse
    {
        public int RowNumber { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
    }

    public class SendLinkingEmailResponse
    {
        public int SentCount { get; set; } = 0;
        public List<Guid> SkippedAccountIds { get; set; } = new();
        public List<Guid> FailedAccountIds { get; set; } = new();
    }

    public class LoginResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SessionId { get; set; } = Guid.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool HasPassword { get; set; } = false;
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChangePasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class GetMyProfileResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateMyProfileResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; } = false;
        public string AcademicStatus { get; set; } = string.Empty;
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class UploadAvatarResponse
    {
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class GetAllAccountRoleResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class GetAllAccountRoleWithPaginationResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class GetAccountRoleByIdResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class SearchAccountRoleResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class CreateAccountRoleResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid RoleId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class UpdateStudentProfileResponse
    {
        public Guid StudentProfileId { get; set; } = Guid.Empty;
        public Guid StudentId { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string AcademicStatus { get; set; } = "enrolled";
    }
}
