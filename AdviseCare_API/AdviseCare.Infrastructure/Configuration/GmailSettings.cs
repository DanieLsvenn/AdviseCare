namespace AdviseCare.Infrastructure.Configuration
{
    // Bound from the "Gmail" section of appsettings.json
    public class GmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string FromName { get; set; } = "AdviseCare";
        // Base URL of the web app, used to build the links inside the emails
        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    }
}
