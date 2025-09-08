using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Ocsp;
using server.DTOs;
using server.Models;
using server.Services;
using server.Utilities;
using System.Data;
using System.Net;
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
        private readonly IMailService _mail;
        private readonly IBlacklistService _blacklistService;
        private readonly IAuthService _authService;

        public AuthController(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration, IMailService mail, IBlacklistService blacklistService, IAuthService authService)
        {
            _supabaseService = supabaseService;
            _tokenService = tokenService;
            _configuration = configuration;
            _mail = mail;
            _blacklistService = blacklistService;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (await _authService.IsEmailTakenAsync(request.Email))
                    return Conflict(new { message = "Email already exists." });

                var createdUser = await _authService.CreateUserAsync(request.Email, request.Password, request.UserType);

                if (createdUser == null)
                {
                    return StatusCode(500, new { message = "Failed to create user due to a server error." });
                }

                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    UserType = createdUser.UserType,
                    CreatedAt = createdUser.CreatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {       
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var user = await _authService.GetUserByEmailAsync(request.Email);

                if (user == null || !_authService.VerifyPassword(request.Password, user.Password))
                    return Unauthorized(new { message = "Invalid email or password." });

                if (await _authService.EnsureAdminVerificationAsync(user))
                    return Conflict(new { message = "Please authenticate your login." });

                var tokenResponse = await _authService.GenerateTokenResponseAsync(user, request.RememberMe);
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.RefreshUsingRefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _authService.LogoutAsync(request.RefreshToken!, request.AccessToken);
                return Ok(new { message = "Logged out successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Refresh token not found." });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { message = "Refresh token already revoked." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyUser([FromBody] VerifyUserDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.VerifyUserAndIssueTokensAsync(request.Email, request.UserCodeVerify, request.RememberMe);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not found." });
            }
            catch (ArgumentException)
            {
                return BadRequest(new { message = "Verify code not matching." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("sendMail")]
        public async Task<IActionResult> SendMail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required." });
            }
            if (!CommonUtils.IsValidEmail(email))
            {
                return BadRequest(new { message = "Email is wrong format." });
            }
            try
            {
                await _authService.ResendVerificationCodeAsync(email);
                return Ok(new { message = "A new verification code has been sent to your email !" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "User not found." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        

    }
}
