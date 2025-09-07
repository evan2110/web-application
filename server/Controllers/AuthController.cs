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
            if (string.IsNullOrEmpty(request.Email.Trim()) || string.IsNullOrEmpty(request.Password.Trim()) || string.IsNullOrEmpty(request.UserType.Trim()))
                return BadRequest(new { message = "Invalid request." });

            if (request.Password.Trim().Length < 6)
            {
                return BadRequest(new { message = "Password must greater than 6." });
            }

            if (!CommonUtils.IsValidEmail(request.Email.Trim()))
            {
                return BadRequest(new { message = "Email is wrong format." });
            }

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
            if (string.IsNullOrEmpty(request.Email.Trim()) || string.IsNullOrEmpty(request.Password.Trim()))
                return BadRequest(new { message = "Email and password are required." });

            if (request.Password.Trim().Length < 6)
            {
                return BadRequest(new { message = "Password must greater than 6." });
            }

            if (!CommonUtils.IsValidEmail(request.Email.Trim()))
            {
                return BadRequest(new { message = "Email is wrong format." });
            }

            var users = await _supabaseService.GetAllAsync<User>();

            if (!users.Any(e => e.Email == request.Email))
                return Unauthorized(new { message = "User not exist." });

            var user = users.FirstOrDefault(e => e.Email == request.Email);

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            if(user.UserType.ToLower() == "admin")
            {
                MailDataReqDTO mailDataReq = new MailDataReqDTO();
                var code = CommonUtils.GenerateVerificationCode();

                var codes = await _supabaseService.GetAllAsync<UserCodeVerify>();
                var codeVerify = codes.FirstOrDefault(e => e.UserId == user.Id);
                if(codeVerify != null)
                {
                    codeVerify.VerifyCode = code;
                    await _supabaseService.UpdateAsync(codeVerify);
                }
                else
                {
                    var userCodeVerify = new UserCodeVerify
                    {
                        Id = 0,
                        UserId = user.Id,
                        VerifyCode = code,
                        Status = 1,
                    };

                    await _supabaseService.CreateAsync(userCodeVerify);
                }

                await _authService.SendVerificationEmailAsync(user.Email, code);
                return Conflict(new
                {
                    message = "Please authenticate your login."
                });
            }

            var tokenResponse = await _authService.GenerateTokenResponseAsync(user, request.RememberMe);
            return Ok(tokenResponse);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken.Trim()))
                return BadRequest(new { message = "Refresh token is required." });

            var refreshTokens = await _supabaseService.GetAllAsync<RefreshToken>();
            var existingToken = refreshTokens.FirstOrDefault(e => e.Token == request.RefreshToken);

            if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.Now)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

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
        public async Task<IActionResult> Logout([FromBody] LogoutDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken.Trim()) || string.IsNullOrWhiteSpace(request.AccessToken.Trim()))
            {
                return BadRequest(new { message = "Invalid request." });
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

            // Add access token to blacklist if provided
            if (!string.IsNullOrWhiteSpace(request.AccessToken))
            {
                await _blacklistService.AddTokenToBlacklistAsync(request.AccessToken, storedToken.UserId, "User logout");
            }

            // Revoke refresh token
            storedToken.RevokedAt = DateTime.Now;
            await _supabaseService.UpdateAsync(storedToken);
            
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyUser([FromBody] VerifyUserDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.UserCodeVerify.Trim()))
            {
                return BadRequest(new { message = "Verify code is required." });
            }
            if (string.IsNullOrWhiteSpace(request.Email.Trim()))
            {

                return BadRequest(new { message = "Email is required." });
            }
            if (!CommonUtils.IsValidEmail(request.Email.Trim()))
            {
                return BadRequest(new { message = "Email is wrong format." });
            }
            if (request.UserCodeVerify.Trim().Length < 6)
            {
                return BadRequest(new { message = "Verify code must greater than 6." });
            }

            var users = await _supabaseService.GetAllAsync<User>();
            var user = users.FirstOrDefault(e => e.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            var codes = await _supabaseService.GetAllAsync<UserCodeVerify>();
            var codeVerify = codes.FirstOrDefault(e => e.UserId == user.Id);

            if (codeVerify.VerifyCode != request.UserCodeVerify.Trim())
            {
                return BadRequest(new { message = "Verify code not matching." });
            }

            var tokenResponse = await _authService.GenerateTokenResponseAsync(user, request.RememberMe);
            return Ok(tokenResponse);
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
            var users = await _supabaseService.GetAllAsync<User>();
            var user = users.FirstOrDefault(e => e.Email == email);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            string code = CommonUtils.GenerateVerificationCode();
            await _authService.SendVerificationEmailAsync(user.Email, code);

            var codes = await _supabaseService.GetAllAsync<UserCodeVerify>();
            var codeVerify = codes.FirstOrDefault(e => e.UserId == user.Id);
            codeVerify.VerifyCode = code;
            await _supabaseService.UpdateAsync(codeVerify);

            return Ok(new { message = "A new verification code has been sent to your email !" });
        }

        

    }
}
