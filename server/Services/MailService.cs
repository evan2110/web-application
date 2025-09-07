using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using server.DTOs;

namespace server.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettingsDTO _settings;

        public MailService(IConfiguration configuration, IOptions<MailSettingsDTO> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(MailDataReqDTO mailData)
        {
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
        }
    }
}
