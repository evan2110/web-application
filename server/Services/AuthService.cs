using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using server.DTOs;
using server.Models;
using System.Security.Claims;

namespace server.Services
{
	public class AuthService : IAuthService
	{
		private readonly ISupabaseService _supabaseService;
		private readonly ITokenService _tokenService;
		private readonly IConfiguration _configuration;
		private readonly IMailService _mailService;
		private readonly IBlacklistService _blacklistService;
		private readonly ILogger<AuthService> _logger;

		public AuthService(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration, IMailService mailService, IBlacklistService blacklistService, ILogger<AuthService> logger)
		{
			_supabaseService = supabaseService;
			_tokenService = tokenService;
			_configuration = configuration;
			_mailService = mailService;
			_blacklistService = blacklistService;
			_logger = logger;
		}

		public async Task<User?> GetUserByEmailAsync(string email)
		{
			_logger.LogDebug("Fetching user by email {Email}", email);
			return await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Single();
		}

		public async Task<bool> IsEmailTakenAsync(string email)
		{
			_logger.LogDebug("Checking if email exists {Email}", email);
			var existing = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Get();
			var taken = existing.Models?.Any() == true;
			if (taken) _logger.LogInformation("Email already exists {Email}", email);
			return taken;
		}

		public bool VerifyPassword(string plainText, string hashed)
		{
			return BCrypt.Net.BCrypt.Verify(plainText, hashed);
		}

		public async Task<User> CreateUserAsync(string email, string password, string? userType)
		{
			_logger.LogInformation("Creating user for {Email}", email);
			string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var verifyToken = _tokenService.GenerateEmailVerificationToken(email);

            var newUser = new User
			{
				Id = 0,
				Email = email,
				Password = passwordHash,
				UserType = userType,
				CreatedAt = DateTime.Now,
				ConfirmedToken = verifyToken
			};

			var createdUser = await _supabaseService.CreateAsync(newUser);
			if (createdUser == null)
			{
				_logger.LogError("Failed to create user for {Email}", email);
				throw new Exception("Failed to create user");
			}
			_logger.LogInformation("User created successfully with id {UserId}", createdUser.Id);

			// Send email verification link for all users upon registration
			var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
			var verifyLink = $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(verifyToken)}";
			await SendEmailVerificationLinkAsync(createdUser.Email, verifyLink);
			return createdUser;
		}

		public async Task<bool> EnsureAdminVerificationAsync(User user)
		{
			if (!string.Equals(user.UserType, Utilities.CommonUtils.UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
				return false;

			_logger.LogInformation("Ensuring admin verification for userId {UserId}", user.Id);
			var code = Utilities.CommonUtils.GenerateVerificationCode();

			var existing = await _supabaseService.GetClient()
				.From<UserCodeVerify>()
				.Where(x => x.UserId == user.Id)
				.Single();

			if (existing != null)
			{
				existing.VerifyCode = code;
				await _supabaseService.UpdateAsync(existing);
				_logger.LogInformation("Updated admin verification code for userId {UserId}", user.Id);
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
				_logger.LogInformation("Created admin verification code for userId {UserId}", user.Id);
			}

			await SendVerificationEmailAsync(user.Email, code);
			_logger.LogInformation("Sent admin verification email to {Email}", user.Email);
			return true;
		}

		public async Task<object> GenerateTokenResponseAsync(User user, bool rememberMe = false)
		{
			_logger.LogInformation("Generating token response for userId {UserId}", user.Id);
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
				ExpiresAt = rememberMe
					? DateTime.Now.AddDays(30)
					: DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays")),
				CreatedAt = DateTime.Now,
				RevokedAt = null
			};

			await _supabaseService.CreateAsync(refreshToken);

			return new
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
			};
		}

		public async Task<object> RefreshUsingRefreshTokenAsync(string refreshTokenValue)
		{
			_logger.LogInformation("Attempting to refresh tokens");
			var tokens = await _supabaseService.GetClient()
				.From<RefreshToken>()
				.Where(t => t.Token == refreshTokenValue)
				.Get();
			var existingToken = tokens.Models?.FirstOrDefault();
			if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.Now)
			{
				_logger.LogWarning("Refresh token invalid or expired");
				throw new UnauthorizedAccessException("Invalid or expired refresh token.");
			}

			var user = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Id == existingToken.UserId)
				.Single();
			if (user == null)
			{
				_logger.LogWarning("User for refresh token not found. userId {UserId}", existingToken.UserId);
				throw new UnauthorizedAccessException("User not found.");
			}

			// Revoke old refresh token
			existingToken.RevokedAt = DateTime.Now;
			await _supabaseService.UpdateAsync(existingToken);
			_logger.LogInformation("Revoked old refresh token for userId {UserId}", user.Id);

			// Create claims and new tokens
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.UserType ?? "")
			};

			var newAccessToken = _tokenService.GenerateAccessToken(claims);
			var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

			var newRefreshToken = new RefreshToken
			{
				Id = 0,
				UserId = user.Id,
				Token = newRefreshTokenValue,
				CreatedAt = DateTime.Now,
				ExpiresAt = DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays"))
			};

			await _supabaseService.CreateAsync(newRefreshToken);
			_logger.LogInformation("Issued new tokens for userId {UserId}", user.Id);

			return new
			{
				access_token = newAccessToken,
				refresh_token = newRefreshTokenValue
			};
		}

		public async Task SendVerificationEmailAsync(string toEmail, string code)
		{
			_logger.LogInformation("Sending verification email to {Email}", toEmail);
			MailDataReqDTO mailDataReq = new MailDataReqDTO();
			mailDataReq.ToEmail = toEmail;
			mailDataReq.Subject = "Verify account";

			string bodyHtml = "<html><body>";
			bodyHtml += "<h1>Login authentication</h1>";
			bodyHtml += "<p>Hello " + toEmail + ",</p>";
			bodyHtml += "<p>Welcome back to login!</p>";
			bodyHtml += "<p>Your verification code is: <strong>" + code + "</strong></p>";
			bodyHtml += "</body></html>";

			mailDataReq.Body = bodyHtml;

			await _mailService.SendAsync(mailDataReq);
		}

		public async Task SendEmailVerificationLinkAsync(string toEmail, string verifyLink)
		{
			_logger.LogInformation("Sending email verification link to {Email}", toEmail);
			MailDataReqDTO mailDataReq = new MailDataReqDTO();
			mailDataReq.ToEmail = toEmail;
			mailDataReq.Subject = "Verify your email";

			string bodyHtml = "<html><body>";
			bodyHtml += "<h1>Email verification</h1>";
			bodyHtml += "<p>Hello " + toEmail + ",</p>";
			bodyHtml += "<p>Please confirm your email by clicking the link below:</p>";
			bodyHtml += $"<p><a href=\"{verifyLink}\">Verify your email</a></p>";
			bodyHtml += "</body></html>";

			mailDataReq.Body = bodyHtml;

			await _mailService.SendAsync(mailDataReq);
		}

		public async Task UpdateUserAsync(User user)
		{
			await _supabaseService.UpdateAsync(user);
		}

		public async Task<object> VerifyUserAndIssueTokensAsync(string email, string verifyCode, bool rememberMe)
		{
			_logger.LogInformation("Verifying user with email {Email}", email);
			var user = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Single();
			if (user == null)
				throw new UnauthorizedAccessException("User not found.");

			var codes = await _supabaseService.GetClient()
				.From<UserCodeVerify>()
				.Where(c => c.UserId == user.Id)
				.Single();
			if (codes == null || string.IsNullOrWhiteSpace(codes.VerifyCode) || codes.VerifyCode.Trim() != verifyCode.Trim())
			{
				_logger.LogWarning("Verify code not matching for userId {UserId}", user.Id);
				throw new ArgumentException("Verify code not matching.");
			}

			_logger.LogInformation("User verified successfully {UserId}", user.Id);

			return await GenerateTokenResponseAsync(user, rememberMe);
		}

		public async Task ResendVerificationCodeAsync(string email)
		{
			_logger.LogInformation("Resending verification code to {Email}", email);
			var user = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Single();
			if (user == null)
				throw new UnauthorizedAccessException("User not found.");

			var code = Utilities.CommonUtils.GenerateVerificationCode();

			var existing = await _supabaseService.GetClient()
				.From<UserCodeVerify>()
				.Where(x => x.UserId == user.Id)
				.Single();

			if (existing != null)
			{
				existing.VerifyCode = code;
				await _supabaseService.UpdateAsync(existing);
				_logger.LogInformation("Updated verification code for userId {UserId}", user.Id);
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
				_logger.LogInformation("Created verification code for userId {UserId}", user.Id);
			}

			await SendVerificationEmailAsync(user.Email, code);
			_logger.LogInformation("Sent verification email to {Email}", user.Email);
		}

		public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
		{
			_logger.LogInformation("Sending password reset email to {Email}", toEmail);
			MailDataReqDTO mailDataReq = new MailDataReqDTO();
			mailDataReq.ToEmail = toEmail;
			mailDataReq.Subject = "Reset your password";

			string bodyHtml = "<html><body>";
			bodyHtml += "<h1>Password reset</h1>";
			bodyHtml += "<p>Hello " + toEmail + ",</p>";
			bodyHtml += "<p>We received a request to reset your password.</p>";
			bodyHtml += $"<p>Click the link below to set a new password (valid for 15 minutes):</p>";
			bodyHtml += $"<p><a href=\"{resetLink}\">Reset your password</a></p>";
			bodyHtml += "</body></html>";

			mailDataReq.Body = bodyHtml;

			await _mailService.SendAsync(mailDataReq);
		}

		public async Task ResetPasswordAsync(string email, string newPassword)
		{
			_logger.LogInformation("Resetting password for {Email}", email);
			var user = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Single();
			if (user == null)
				throw new UnauthorizedAccessException("User not found.");

			user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
			await _supabaseService.UpdateAsync(user);
			_logger.LogInformation("Password reset successfully for {Email}", email);
		}

		public async Task LogoutAsync(string refreshToken, string? accessToken)
		{
			_logger.LogInformation("Logout requested");
			var tokens = await _supabaseService.GetClient()
				.From<RefreshToken>()
				.Where(t => t.Token == refreshToken)
				.Get();
			var storedToken = tokens.Models?.FirstOrDefault();
			if (storedToken == null)
			{
				_logger.LogWarning("Refresh token not found during logout");
				throw new KeyNotFoundException("Refresh token not found.");
			}
			if (storedToken.RevokedAt != null)
			{
				_logger.LogWarning("Refresh token already revoked");
				throw new InvalidOperationException("Refresh token already revoked.");
			}

			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				await _blacklistService.AddTokenToBlacklistAsync(accessToken, storedToken.UserId, "User logout");
				_logger.LogInformation("Access token blacklisted for userId {UserId}", storedToken.UserId);
			}

			storedToken.RevokedAt = DateTime.Now;
			await _supabaseService.UpdateAsync(storedToken);
			_logger.LogInformation("Refresh token revoked for userId {UserId}", storedToken.UserId);
		}
	}
}


