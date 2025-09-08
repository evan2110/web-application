using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace server.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IBlacklistService _blacklistService;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, IBlacklistService blacklistService, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _blacklistService = blacklistService;
            _logger = logger;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            _logger.LogDebug("Generating access token with {ClaimCount} claims", claims.Count());
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Int32.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug("Access token generated");
            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            _logger.LogDebug("Generating refresh token");
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public bool ValidateAccessToken(string token)
        {
            _logger.LogDebug("Validating access token");
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Not allow delay time

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Optional: Forsure token is JWT
                if (!(validatedToken is JwtSecurityToken jwtToken))
                    return false;

                return true;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Access token failed validation");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while validating access token");
                return false;
            }
        }

        public async Task<bool> ValidateAccessTokenWithBlacklistAsync(string token)
        {
            // First validate the token structure and signature
            if (!ValidateAccessToken(token))
                return false;

            // Then check if token is blacklisted
            var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(token);
            if (isBlacklisted) _logger.LogWarning("Token is blacklisted");
            return !isBlacklisted;
        }
    }
}
