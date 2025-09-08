using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using server.Services;

namespace test.ServiceTests
{
    public class BlacklistCleanupServiceTests
    {
        [Fact]
        public async Task Executes_Cleanup_And_Honors_Cancellation()
        {
            var services = new ServiceCollection();
            var blacklist = new Mock<IBlacklistService>();
            services.AddSingleton(blacklist.Object);
            var provider = services.BuildServiceProvider();
            var logger = new Mock<ILogger<BlacklistCleanupService>>();
            var svc = new BlacklistCleanupService(provider, logger.Object);

            using var cts = new CancellationTokenSource();
            var runTask = svc.StartAsync(cts.Token);

            await Task.Delay(50);
            cts.Cancel();
            await svc.StopAsync(CancellationToken.None);

            blacklist.Verify(b => b.CleanupExpiredTokensAsync(), Times.AtLeastOnce);
        }
    }
}


