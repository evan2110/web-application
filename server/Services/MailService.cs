using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MimeKit;
using server.DTOs;

namespace server.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettingsDTO _settings;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration configuration, IOptions<MailSettingsDTO> settings, ILogger<MailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(MailDataReqDTO mailData)
        {
            _logger.LogInformation("Sending email to {ToEmail} with subject {Subject}", mailData.ToEmail, mailData.Subject);
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_settings.Email);
            email.To.Add(MailboxAddress.Parse(mailData.ToEmail));
            email.Subject = mailData.Subject;
            var builder = new BodyBuilder();
            builder.HtmlBody = mailData.Body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_settings.Email, _settings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
            _logger.LogInformation("Email sent to {ToEmail}", mailData.ToEmail);
        }
    }
}
