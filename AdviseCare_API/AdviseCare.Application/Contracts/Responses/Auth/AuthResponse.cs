namespace AdviseCare.Application.Contracts.Responses.Auth
{
    public class LoginResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid SessionId { get; set; } = Guid.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public class GetSelfProfileResponse
    {
        public Guid AccountId { get; set; } = Guid.Empty;
        public Guid? FacultyId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string LinkStatus { get; set; } = "notLinked";
        public string PreferredLanguage { get; set; } = "vi";
        public string Status { get; set; } = "active";
        public List<string> Roles { get; set; } = new();
    }

    public class LogoutResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
