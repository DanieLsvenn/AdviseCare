using AdviseCare.Application.Interfaces.Email;
using AdviseCare.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AdviseCare.Infrastructure.Email
{
    // Sends emails through Gmail SMTP (smtp.gmail.com:587, STARTTLS) with a Google app password
    public class EmailService : IEmailService
    {
        private readonly GmailSettings _settings;

        public EmailService(IOptions<GmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAccountLinkingEmailAsync(string toEmail, string fullName, string token)
        {
            var link = $"{_settings.FrontendBaseUrl.TrimEnd('/')}/link-account?token={Uri.EscapeDataString(token)}";
            var body = $"""
                <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                <p>An AdviseCare account has been created for you. Open the link below and sign in with the Google account of <b>{WebUtility.HtmlEncode(toEmail)}</b> to link it.</p>
                <p><a href="{link}">Link my AdviseCare account</a></p>
                <p>This link works once and expires in 7 days. If it has expired, ask the System Administrator to send a new one.</p>
                """;
            await SendAsync(toEmail, "Link your AdviseCare account", body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string token)
        {
            var link = $"{_settings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
            var body = $"""
                <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                <p>We received a request to reset your AdviseCare password.</p>
                <p><a href="{link}">Set a new password</a></p>
                <p>This link works once and expires in 1 hour. If you did not ask for it, you can ignore this email.</p>
                """;
            await SendAsync(toEmail, "Reset your AdviseCare password", body);
        }

        public async Task SendNotificationEmailAsync(string toEmail, string fullName, string title, string message, string? redirectUrl)
        {
            var link = string.IsNullOrWhiteSpace(redirectUrl)
                ? string.Empty
                : $"<p><a href=\"{_settings.FrontendBaseUrl.TrimEnd('/')}/{redirectUrl.TrimStart('/')}\">Open in AdviseCare</a></p>";
            var body = $"""
                <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                <p>{WebUtility.HtmlEncode(message)}</p>
                {link}
                <p>You can turn off email notifications in your AdviseCare settings.</p>
                """;
            await SendAsync(toEmail, $"AdviseCare: {title}", body);
        }

        // Helper: sends one HTML email from the configured Gmail account
        private async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.Username, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_settings.Username, _settings.AppPassword)
            };
            await client.SendMailAsync(message);
        }
    }
}
