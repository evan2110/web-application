using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using server.Models;
using server.Services;

namespace test.ServiceTests
{
    public class BlacklistServiceTests
    {
        private (BlacklistService service, Mock<ISupabaseService> supabase, IConfiguration config, Mock<ILogger<BlacklistService>> logger) Build()
        {
            var supabase = new Mock<ISupabaseService>();
            var settings = new Dictionary<string, string?>
            {
                { "Jwt:AccessTokenExpirationMinutes", "60" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
            var logger = new Mock<ILogger<BlacklistService>>();
            var service = new BlacklistService(supabase.Object, config, logger.Object);
            return (service, supabase, config, logger);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_ReturnsTrue_WhenFoundAndNotExpired()
        {
            var (service, supabase, _, _) = Build();
            supabase.Setup(s => s.GetAllAsync<BlacklistedToken>()).ReturnsAsync(new List<BlacklistedToken> {
                new BlacklistedToken { Id = 1, Token = "abc", BlacklistedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddMinutes(10) }
            });

            var result = await service.IsTokenBlacklistedAsync("abc");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AddTokenToBlacklistAsync_CreatesRecord()
        {
            var (service, supabase, _, _) = Build();
            supabase.Setup(s => s.CreateAsync(It.IsAny<BlacklistedToken>()))
                .ReturnsAsync((BlacklistedToken bt) => { bt.Id = 10; return bt; });

            var ok = await service.AddTokenToBlacklistAsync("header.payload.sig", 3, "logout");
            ok.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveTokenFromBlacklistAsync_Removes_WhenExists()
        {
            var (service, supabase, _, _) = Build();
            var existing = new BlacklistedToken { Id = 5, Token = "tok", BlacklistedAt = DateTime.Now };
            supabase.Setup(s => s.GetAllAsync<BlacklistedToken>()).ReturnsAsync(new List<BlacklistedToken> { existing });
            supabase.Setup(s => s.DeleteAsync<BlacklistedToken>(existing.Id)).ReturnsAsync(true);

            var ok = await service.RemoveTokenFromBlacklistAsync("tok");
            ok.Should().BeTrue();
        }

        [Fact]
        public async Task CleanupExpiredTokensAsync_DeletesExpired()
        {
            var (service, supabase, _, _) = Build();
            var expired = new BlacklistedToken { Id = 7, Token = "e", BlacklistedAt = DateTime.Now.AddHours(-2), ExpiresAt = DateTime.Now.AddMinutes(-1) };
            var active = new BlacklistedToken { Id = 8, Token = "a", BlacklistedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddMinutes(5) };
            supabase.Setup(s => s.GetAllAsync<BlacklistedToken>()).ReturnsAsync(new List<BlacklistedToken> { expired, active });
            supabase.Setup(s => s.DeleteAsync<BlacklistedToken>(expired.Id)).ReturnsAsync(true);

            await service.CleanupExpiredTokensAsync();
            supabase.Verify(s => s.DeleteAsync<BlacklistedToken>(expired.Id), Times.Once);
        }
    }
}

