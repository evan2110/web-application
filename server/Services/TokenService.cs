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

        public string GeneratePasswordResetToken(string email)
        {
            _logger.LogDebug("Generating password reset token for {Email}", email);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("typ", "pwdreset")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug("Password reset token generated");
            return tokenString;
        }

        public bool TryValidatePasswordResetToken(string token, out string email)
        {
            email = string.Empty;
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
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                if (validatedToken is not JwtSecurityToken jwt)
                    return false;

                var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "typ")?.Value;
                if (typeClaim != "pwdreset")
                    return false;

                email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
                return !string.IsNullOrWhiteSpace(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset token failed validation");
                return false;
            }
        }

        public string GenerateEmailVerificationToken(string email)
        {
            _logger.LogDebug("Generating email verification token for {Email}", email);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("typ", "emailverify")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug("Email verification token generated");
            return tokenString;
        }

        public bool TryValidateEmailVerificationToken(string token, out string email)
        {
            email = string.Empty;
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
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                if (validatedToken is not JwtSecurityToken jwt)
                    return false;

                var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "typ")?.Value;
                if (typeClaim != "emailverify")
                    return false;

                email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
                return !string.IsNullOrWhiteSpace(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email verification token failed validation");
                return false;
            }
        }
    }
}
