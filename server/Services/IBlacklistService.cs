using server.Models;

namespace server.Services
{
    public interface IBlacklistService
    {
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task<bool> AddTokenToBlacklistAsync(string token, int? userId = null, string? reason = null);
        Task<bool> RemoveTokenFromBlacklistAsync(string token);
        Task CleanupExpiredTokensAsync();
    }
}
