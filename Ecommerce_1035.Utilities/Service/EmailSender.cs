using Ecommerce_1035.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Ecommerce_1035.Utilities.Service
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                string toEmail = string.IsNullOrEmpty(email)
                    ? _emailSettings.ToEmail
                    : email;

                using var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.UsernameEmail, "Book Store"),
                    Subject = "Book Store " + subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mail.To.Add(toEmail);

                // Only add CC if a valid address is configured
                if (!string.IsNullOrWhiteSpace(_emailSettings.CcEmail))
                {
                    mail.CC.Add(_emailSettings.CcEmail);
                }

                using var smtp = new SmtpClient(_emailSettings.PrimaryDomain, _emailSettings.PrimaryPort)
                {
                    Credentials = new NetworkCredential(
                        _emailSettings.UsernameEmail,
                        _emailSettings.UsernamePassword),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Confirmation email successfully dispatched to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. Error: {Message}", email, ex.Message);
                throw; // Rethrow so you immediately see the issue in the console/debug window
            }
        }
    }
}