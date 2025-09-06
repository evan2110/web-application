using System.Security.Claims;

namespace server.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
    }
}
