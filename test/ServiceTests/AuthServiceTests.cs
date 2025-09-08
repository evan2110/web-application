using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using server.DTOs;
using server.Models;
using server.Services;

namespace test.ServiceTests
{
    public class AuthServiceTests
    {
        private (AuthService service,
            Mock<ISupabaseService> supabase,
            Mock<ITokenService> token,
            Mock<IMailService> mail,
            Mock<IBlacklistService> blacklist,
            IConfiguration config) Build()
        {
            var supabase = new Mock<ISupabaseService>();
            var token = new Mock<ITokenService>();
            var mail = new Mock<IMailService>();
            var blacklist = new Mock<IBlacklistService>();
            var logger = new Mock<ILogger<AuthService>>();

            var settings = new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenExpirationDays", "7" },
                { "Backend:BaseUrl", "https://backend.local" },
                { "Jwt:Issuer", "issuer" },
                { "Jwt:Audience", "aud" },
                { "Jwt:SecretKey", new string('k', 64) }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();

            var service = new AuthService(supabase.Object, token.Object, config, mail.Object, blacklist.Object, logger.Object);
            return (service, supabase, token, mail, blacklist, config);
        }

        [Fact]
        public void VerifyPassword_Works_ForValidAndInvalid()
        {
            var (svc, _, _, _, _, _) = Build();
            var hash = BCrypt.Net.BCrypt.HashPassword("secret");
            svc.VerifyPassword("secret", hash).Should().BeTrue();
            svc.VerifyPassword("bad", hash).Should().BeFalse();
        }

        [Fact]
        public async Task CreateUserAsync_Creates_And_Sends_Verification_Link()
        {
            var (svc, supabase, token, mail, _, config) = Build();
            token.Setup(t => t.GenerateEmailVerificationToken("u@x.com")).Returns("tok");
            supabase.Setup(s => s.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 10; return u; });

            var created = await svc.CreateUserAsync("u@x.com", "123456", "user");

            created.Should().NotBeNull();
            created.Email.Should().Be("u@x.com");
            mail.Verify(m => m.SendAsync(It.Is<MailDataReqDTO>(d => d.ToEmail == "u@x.com" && d.Subject.Contains("Verify"))), Times.Once);
        }

        [Fact]
        public async Task GenerateTokenResponseAsync_ReturnsTokens_And_StoresRefreshToken()
        {
            var (svc, supabase, token, _, _, config) = Build();
            token.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("access");
            token.Setup(t => t.GenerateRefreshToken()).Returns("refresh");
            supabase.Setup(s => s.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => { rt.Id = 1; return rt; });

            var user = new User { Id = 7, Email = "u@x.com", UserType = "user", CreatedAt = DateTime.Now };
            var obj = await svc.GenerateTokenResponseAsync(user, rememberMe: true);

            var type = obj.GetType();
            var access = type.GetProperty("access_token")!.GetValue(obj) as string;
            var refresh = type.GetProperty("refresh_token")!.GetValue(obj) as string;
            access.Should().Be("access");
            refresh.Should().Be("refresh");
            supabase.Verify(s => s.CreateAsync(It.Is<RefreshToken>(r => r.UserId == 7 && r.Token == "refresh")), Times.Once);
        }

        [Fact]
        public async Task SendVerificationEmailAsync_ComposesAndSends()
        {
            var (svc, _, _, mail, _, _) = Build();
            await svc.SendVerificationEmailAsync("u@x.com", "123456");
            mail.Verify(m => m.SendAsync(It.Is<MailDataReqDTO>(d => d.ToEmail == "u@x.com" && d.Subject.Contains("Verify"))), Times.Once);
        }

        [Fact]
        public async Task SendEmailVerificationLinkAsync_ComposesAndSends()
        {
            var (svc, _, _, mail, _, _) = Build();
            await svc.SendEmailVerificationLinkAsync("u@x.com", "https://link");
            mail.Verify(m => m.SendAsync(It.Is<MailDataReqDTO>(d => d.ToEmail == "u@x.com" && d.Body.Contains("https://link"))), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_Calls_Update()
        {
            var (svc, supabase, _, _, _, _) = Build();
            var user = new User { Id = 1, Email = "a@b.com" };
            supabase.Setup(s => s.UpdateAsync(user)).ReturnsAsync(user);
            await svc.UpdateUserAsync(user);
            supabase.Verify(s => s.UpdateAsync(user), Times.Once);
        }
    }
}


