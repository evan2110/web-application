using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthController> _logger;
        private readonly IMessageProvider _messages;

        public AuthController(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration, IMailService mail, IBlacklistService blacklistService, IAuthService authService, ILogger<AuthController> logger, IMessageProvider messages)
        {
            _supabaseService = supabaseService;
            _tokenService = tokenService;
            _configuration = configuration;
            _mail = mail;
            _blacklistService = blacklistService;
            _authService = authService;
            _logger = logger;
            _messages = messages;
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
                _logger.LogInformation("Register requested for {Email}", request.Email);
                if (await _authService.IsEmailTakenAsync(request.Email))
                    return Conflict(new { message = _messages.Get(CommonUtils.MessageCodes.EmailAlreadyExists) });

                var createdUser = await _authService.CreateUserAsync(request.Email, request.Password, request.UserType);

                if (createdUser == null)
                {
                    return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.FailedCreateUser) });
                }

                return Ok(new { message = "Registration successful. Please check your email to verify your account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during register for {Email}", request.Email);
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
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
                _logger.LogInformation("Login requested for {Email}", request.Email);
                var user = await _authService.GetUserByEmailAsync(request.Email);

                if (user == null || !_authService.VerifyPassword(request.Password, user.Password))
                    return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.InvalidEmailOrPassword) });

                if (user.VerifyAt == null)
                    return Unauthorized(new { message = "Please verify your email before logging in." });

                if (await _authService.EnsureAdminVerificationAsync(user))
                    return Conflict(new { message = _messages.Get(CommonUtils.MessageCodes.PleaseAuthenticateLogin) });

                var tokenResponse = await _authService.GenerateTokenResponseAsync(user, request.RememberMe);
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
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
                _logger.LogInformation("Refresh token requested");
                var result = await _authService.RefreshUsingRefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.InvalidOrExpiredRefreshToken) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
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
                _logger.LogInformation("Logout requested");
                await _authService.LogoutAsync(request.RefreshToken!, request.AccessToken);
                return Ok(new { message = _messages.Get(CommonUtils.MessageCodes.LoggedOutSucessfully) });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Refresh token not found during logout");
                return NotFound(new { message = _messages.Get(CommonUtils.MessageCodes.RefreshTokenNotFound) });
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Refresh token already revoked during logout");
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.RefreshTokenAlreadyRevoked) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
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
                _logger.LogInformation("Verify requested for {Email}", request.Email);
                var result = await _authService.VerifyUserAndIssueTokensAsync(request.Email, request.UserCodeVerify, request.RememberMe);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User not found during verify for {Email}", request.Email);
                return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.UserNotFound) });
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Verify code mismatch for {Email}", request.Email);
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.VerifyCodeNotMatching) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during verify for {Email}", request.Email);
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Invalid verification link." });

            try
            {
                if (!_tokenService.TryValidateEmailVerificationToken(token, out var email))
                    return BadRequest(new { message = "Invalid or expired verification link." });

                var user = await _authService.GetUserByEmailAsync(email);
                if (user == null)
                    return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.UserNotFound) });

                user.VerifyAt = DateTime.Now;
                await _authService.UpdateUserAsync(user);

                var configuredFrontend = _configuration["Frontend:BaseUrl"];
                var frontendBaseUrl = !string.IsNullOrWhiteSpace(configuredFrontend)
                    ? configuredFrontend
                    : (Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:3000");
                var redirectUrl = $"{frontendBaseUrl}/login?verified=true";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }

        [HttpGet("sendMail")]
        public async Task<IActionResult> SendMail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.EmailRequired) });
            }
            if (!CommonUtils.IsValidEmail(email))
            {
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.EmailWrongFormat) });
            }
            try
            {
                _logger.LogInformation("Resend verification requested for {Email}", email);
                await _authService.ResendVerificationCodeAsync(email);
                return Ok(new { message = _messages.Get(CommonUtils.MessageCodes.VerifyCodeSent) });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User not found during resend for {Email}", email);
                return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.UserNotFound) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resend for {Email}", email);
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.EmailRequired) });
            }
            if (!CommonUtils.IsValidEmail(request.Email))
            {
                return BadRequest(new { message = _messages.Get(CommonUtils.MessageCodes.EmailWrongFormat) });
            }
            try
            {
                _logger.LogInformation("Password reset requested for {Email}", request.Email);
                var user = await _authService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.UserNotFound) });
                }
                if (user.VerifyAt == null)
                {
                    return Unauthorized(new { message = "Please verify your email before requesting a password reset." });
                }
                var resetToken = _tokenService.GeneratePasswordResetToken(request.Email);
                var frontendBaseUrl = Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:3000";
                var resetLink = $"{frontendBaseUrl}/reset-password?token={WebUtility.UrlEncode(resetToken)}";
                await _authService.SendPasswordResetEmailAsync(request.Email, resetLink);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot-password for {Email}", request.Email);
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (!_tokenService.TryValidatePasswordResetToken(request.Token, out var email))
                {
                    return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.InvalidOrExpiredRefreshToken) });
                }
                await _authService.ResetPasswordAsync(email, request.NewPassword);
                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = _messages.Get(CommonUtils.MessageCodes.UserNotFound) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }

    }
}
