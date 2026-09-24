using System.Net;
using System.Net.Mail;

namespace BDTechMarket.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IConfiguration configuration,
            ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            var smtpHost =
                _configuration["EmailSettings:SmtpServer"];

            var smtpPort =
                _configuration.GetValue<int>(
                    "EmailSettings:Port");

            var smtpUsername =
                _configuration["EmailSettings:Username"];

            var smtpPassword =
                _configuration["EmailSettings:Password"];

            var fromEmail =
                _configuration["EmailSettings:SenderEmail"];

            var fromName =
                _configuration["EmailSettings:SenderName"];

            if (string.IsNullOrWhiteSpace(smtpHost) ||
                smtpPort <= 0 ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException(
                    "Email SMTP configuration is missing.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(
                    fromEmail,
                    string.IsNullOrWhiteSpace(fromName)
                        ? "BDTechMarket"
                        : fromName),

                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(email);

            using var smtp = new SmtpClient(
                smtpHost,
                smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    smtpUsername,
                    smtpPassword)
            };

            await smtp.SendMailAsync(message);

            _logger.LogInformation(
                "Email sent successfully to {Email}",
                email);
        }
    }
}