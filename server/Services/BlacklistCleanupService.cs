using server.Services;

namespace server.Services
{
    public class BlacklistCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BlacklistCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Cleanup every hour

        public BlacklistCleanupService(IServiceProvider serviceProvider, ILogger<BlacklistCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();
                        await blacklistService.CleanupExpiredTokensAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during blacklist cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
