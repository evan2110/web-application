using System.Security.Claims;

namespace server.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
        Task<bool> ValidateAccessTokenWithBlacklistAsync(string token);
        string GeneratePasswordResetToken(string email);
        bool TryValidatePasswordResetToken(string token, out string email);
        string GenerateEmailVerificationToken(string email);
        bool TryValidateEmailVerificationToken(string token, out string email);
    }
}
