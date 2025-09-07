using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using server.Services;
using System.Security.Claims;

namespace test.ServiceTests
{
    public class TokenServiceTests
    {
        private (TokenService service, Mock<IBlacklistService> blacklist, IConfiguration config) Build()
        {
            var blacklist = new Mock<IBlacklistService>();
            var settings = new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "issuer" },
                { "Jwt:Audience", "aud" },
                { "Jwt:SecretKey", new string('b', 64) },
                { "Jwt:AccessTokenExpirationMinutes", "5" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
            var service = new TokenService(config, blacklist.Object);
            return (service, blacklist, config);
        }

        [Fact]
        public void GenerateRefreshToken_Returns_Base64String()
        {
            var (service, _, _) = Build();
            var token = service.GenerateRefreshToken();
            token.Should().NotBeNullOrWhiteSpace();
            FluentActions.Invoking(() => Convert.FromBase64String(token)).Should().NotThrow();
        }

        [Fact]
        public void GenerateAccessToken_And_ValidateAccessToken_Work()
        {
            var (service, _, _) = Build();
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "x@y.com") };
            var access = service.GenerateAccessToken(claims);
            access.Should().NotBeNullOrWhiteSpace();
            service.ValidateAccessToken(access).Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAccessTokenWithBlacklistAsync_Respects_Blacklist()
        {
            var (service, blacklist, _) = Build();
            var access = service.GenerateAccessToken(new List<Claim>());
            blacklist.Setup(b => b.IsTokenBlacklistedAsync(access)).ReturnsAsync(true);
            var ok = await service.ValidateAccessTokenWithBlacklistAsync(access);
            ok.Should().BeFalse();
        }
    }
}

