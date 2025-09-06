using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using server.DTOs;
using server.Models;
using server.Services;
using System.Data;
using System.Security.Claims;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISupabaseService _supabaseService;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthController(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration)
        {
            _supabaseService = supabaseService;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var users = await _supabaseService.GetAllAsync<User>();
            if (users.Any(e => e.Email == request.Email))
                return Conflict(new { message = "Email is already in use." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Id = 0,
                Email = request.Email,
                Password = passwordHash,
                UserType = request.UserType,
                CreatedAt = DateTime.Now,
            };

            var createdUser = await _supabaseService.CreateAsync(newUser);

            if (createdUser != null)
            {
                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    Password = createdUser.Password,
                    UserType = createdUser.UserType,
                    CreatedAt = createdUser.CreatedAt
                };

                return Ok(userDto);
            }

            return BadRequest("Failed to create user");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Email and password are required." });

            var users = await _supabaseService.GetAllAsync<User>();

            if (!users.Any(e => e.Email == request.Email))
                return Unauthorized(new { message = "User not exist." });

            var user = users.FirstOrDefault(e => e.Email == request.Email);

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType ?? ""),
            };

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Id = 0,
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = request.RememberMe
                    ? DateTime.Now.AddDays(30)
                    : DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays")),
                CreatedAt = DateTime.Now,
                RevokedAt = null
            };

            await _supabaseService.CreateAsync(refreshToken);
            
            return Ok(new
            {
                user = new
                {
                    user.Id,
                    user.Email,
                    user.UserType,
                    user.CreatedAt,
                },
                access_token = accessToken,
                refresh_token = refreshTokenValue
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            var refreshTokens = await _supabaseService.GetAllAsync<RefreshToken>();
            var existingToken = refreshTokens.FirstOrDefault(e => e.Token == request.RefreshToken);

            if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.Now)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            // Lấy user thông qua UserId từ refresh token
            var users = await _supabaseService.GetAllAsync<User>();
            var user = users.FirstOrDefault(e => e.Id == existingToken.UserId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            // Create claims for access token new
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType ?? "")
            };

            var newAccessToken = _tokenService.GenerateAccessToken(claims);
            var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

            // Revoke refresh token old
            existingToken.RevokedAt = DateTime.Now;
            await _supabaseService.UpdateAsync(existingToken);

            // Create refresh token new
            var newRefreshToken = new RefreshToken
            {
                Id = 0,
                UserId = user.Id,
                Token = newRefreshTokenValue,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))
            };

            await _supabaseService.CreateAsync(newRefreshToken);

            return Ok(new
            {
                access_token = newAccessToken,
                refresh_token = newRefreshTokenValue
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required." });
            }

            var refreshTokens = await _supabaseService.GetAllAsync<RefreshToken>();
            var storedToken = refreshTokens.FirstOrDefault(e => e.Token == request.RefreshToken);

            if (storedToken == null)
            {
                return NotFound(new { message = "Refresh token not found." });
            }

            // If was revoked 
            if (storedToken.RevokedAt != null)
            {
                return BadRequest(new { message = "Refresh token already revoked." });
            }

            storedToken.RevokedAt = DateTime.Now;
            await _supabaseService.UpdateAsync(storedToken);
            return Ok(new { message = "Logged out successfully." });
        }

    }
}
