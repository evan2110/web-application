using Microsoft.IdentityModel.Tokens;
using server.Models;
using System.IdentityModel.Tokens.Jwt;

namespace server.Services
{
    public class BlacklistService : IBlacklistService
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BlacklistService> _logger;

        public BlacklistService(ISupabaseService supabaseService, IConfiguration configuration, ILogger<BlacklistService> logger)
        {
            _supabaseService = supabaseService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            try
            {
                var blacklistedTokens = await _supabaseService.GetAllAsync<BlacklistedToken>();
                var blacklistedToken = blacklistedTokens.FirstOrDefault(bt => bt.Token == token);
                
                if (blacklistedToken == null)
                    return false;

                // Check if token is expired
                if (blacklistedToken.ExpiresAt.HasValue && blacklistedToken.ExpiresAt.Value <= DateTime.Now)
                {
                    // Remove expired token from blacklist
                    await RemoveTokenFromBlacklistAsync(token);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is blacklisted");
                return false; // In case of error, allow token to pass (fail open)
            }
        }

        public async Task<bool> AddTokenToBlacklistAsync(string token, int? userId = null, string? reason = null)
        {
            try
            {
                // Extract expiration time from JWT token
                DateTime? expiresAt = null;
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    expiresAt = jwtToken.ValidTo;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract expiration from token, using default expiration");
                    // If we can't extract expiration, set a default expiration time
                    expiresAt = DateTime.Now.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60));
                }

                var blacklistedToken = new BlacklistedToken
                {
                    Id = 0,
                    Token = token,
                    BlacklistedAt = DateTime.Now,
                    ExpiresAt = expiresAt,
                    UserId = userId,
                    Reason = reason ?? "User logout"
                };

                await _supabaseService.CreateAsync(blacklistedToken);
                _logger.LogInformation("Token blacklisted successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding token to blacklist");
                return false;
            }
        }

        public async Task<bool> RemoveTokenFromBlacklistAsync(string token)
        {
            try
            {
                var blacklistedTokens = await _supabaseService.GetAllAsync<BlacklistedToken>();
                var blacklistedToken = blacklistedTokens.FirstOrDefault(bt => bt.Token == token);
                
                if (blacklistedToken != null)
                {
                    await _supabaseService.DeleteAsync<BlacklistedToken>(blacklistedToken.Id);
                    _logger.LogInformation("Token removed from blacklist successfully");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing token from blacklist");
                return false;
            }
        }

        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                var blacklistedTokens = await _supabaseService.GetAllAsync<BlacklistedToken>();
                var expiredTokens = blacklistedTokens.Where(bt => 
                    bt.ExpiresAt.HasValue && bt.ExpiresAt.Value <= DateTime.Now).ToList();

                foreach (var expiredToken in expiredTokens)
                {
                    await _supabaseService.DeleteAsync<BlacklistedToken>(expiredToken.Id);
                }

                if (expiredTokens.Any())
                {
                    _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired blacklisted tokens");
            }
        }
    }
}
