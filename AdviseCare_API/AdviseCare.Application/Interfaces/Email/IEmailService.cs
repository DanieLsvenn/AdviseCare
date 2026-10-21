namespace AdviseCare.Application.Interfaces.Email
{
    public interface IEmailService
    {
        // The token is the raw one-time token; the email builds the web app link from it
        Task SendAccountLinkingEmailAsync(string toEmail, string fullName, string token);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string token);
        Task SendNotificationEmailAsync(string toEmail, string fullName, string title, string message, string? redirectUrl);
    }
}
