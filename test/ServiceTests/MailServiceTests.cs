using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using server.DTOs;
using server.Services;

namespace test.ServiceTests
{
    public class MailServiceTests
    {
        private (MailService service, IOptions<MailSettingsDTO> options) Build()
        {
            var logger = new Mock<ILogger<MailService>>();
            var settings = Options.Create(new MailSettingsDTO
            {
                Email = "noreply@example.com",
                Password = "pwd",
                DisplayName = "App",
                Host = "localhost",
                Port = 2525
            });
            var config = new ConfigurationBuilder().Build();
            var service = new MailService(config, settings, logger.Object);
            return (service, settings);
        }

        [Fact(Skip = "Requires SMTP server; covered indirectly via AuthService email composition")]
        public async Task SendAsync_Attempts_To_Send()
        {
            var (svc, _) = Build();
            var data = new MailDataReqDTO { ToEmail = "u@x.com", Subject = "Hello", Body = "<b>Hi</b>" };
            await svc.SendAsync(data);
            true.Should().BeTrue();
        }
    }
}


