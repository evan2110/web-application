using Microsoft.AspNetCore.Mvc;
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

        public AuthController(ISupabaseService supabaseService, ITokenService tokenService)
        {
            _supabaseService = supabaseService;
            _tokenService = tokenService;
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
                CreatedAt = DateTime.UtcNow,
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
                    : DateTime.Now.AddDays(1),
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

    }
}
