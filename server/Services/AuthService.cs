using Microsoft.Extensions.Configuration;
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

		public AuthService(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration, IMailService mailService, IBlacklistService blacklistService)
		{
			_supabaseService = supabaseService;
			_tokenService = tokenService;
			_configuration = configuration;
			_mailService = mailService;
			_blacklistService = blacklistService;
		}

		public async Task<User?> GetUserByEmailAsync(string email)
		{
			return await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Single();
		}

		public async Task<bool> IsEmailTakenAsync(string email)
		{
			var existing = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Email == email)
				.Get();
			return existing.Models?.Any() == true;
		}

		public bool VerifyPassword(string plainText, string hashed)
		{
			return BCrypt.Net.BCrypt.Verify(plainText, hashed);
		}

		public async Task<User> CreateUserAsync(string email, string password, string? userType)
		{
			string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
			var newUser = new User
			{
				Id = 0,
				Email = email,
				Password = passwordHash,
				UserType = userType,
				CreatedAt = DateTime.Now,
			};

			var createdUser = await _supabaseService.CreateAsync(newUser);
			if (createdUser == null)
				throw new Exception("Failed to create user");
			return createdUser;
		}

		public async Task<bool> EnsureAdminVerificationAsync(User user)
		{
			if (!string.Equals(user.UserType, "admin", StringComparison.OrdinalIgnoreCase))
				return false;

			var code = Utilities.CommonUtils.GenerateVerificationCode();

			var existing = await _supabaseService.GetClient()
				.From<UserCodeVerify>()
				.Where(x => x.UserId == user.Id)
				.Single();

			if (existing != null)
			{
				existing.VerifyCode = code;
				await _supabaseService.UpdateAsync(existing);
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

			await SendVerificationEmailAsync(user.Email, code);
			return true;
		}

		public async Task<object> GenerateTokenResponseAsync(User user, bool rememberMe = false)
		{
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
			var tokens = await _supabaseService.GetClient()
				.From<RefreshToken>()
				.Where(t => t.Token == refreshTokenValue)
				.Get();
			var existingToken = tokens.Models?.FirstOrDefault();
			if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.Now)
			{
				throw new UnauthorizedAccessException("Invalid or expired refresh token.");
			}

			var user = await _supabaseService.GetClient()
				.From<User>()
				.Where(u => u.Id == existingToken.UserId)
				.Single();
			if (user == null)
			{
				throw new UnauthorizedAccessException("User not found.");
			}

			// Revoke old refresh token
			existingToken.RevokedAt = DateTime.Now;
			await _supabaseService.UpdateAsync(existingToken);

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

			return new
			{
				access_token = newAccessToken,
				refresh_token = newRefreshTokenValue
			};
		}

		public async Task SendVerificationEmailAsync(string toEmail, string code)
		{
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

		public async Task<object> VerifyUserAndIssueTokensAsync(string email, string verifyCode, bool rememberMe)
		{
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
				throw new ArgumentException("Verify code not matching.");

			return await GenerateTokenResponseAsync(user, rememberMe);
		}

		public async Task ResendVerificationCodeAsync(string email)
		{
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

			await SendVerificationEmailAsync(user.Email, code);
		}

		public async Task LogoutAsync(string refreshToken, string? accessToken)
		{
			var tokens = await _supabaseService.GetClient()
				.From<RefreshToken>()
				.Where(t => t.Token == refreshToken)
				.Get();
			var storedToken = tokens.Models?.FirstOrDefault();
			if (storedToken == null)
			{
				throw new KeyNotFoundException("Refresh token not found.");
			}
			if (storedToken.RevokedAt != null)
			{
				throw new InvalidOperationException("Refresh token already revoked.");
			}

			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				await _blacklistService.AddTokenToBlacklistAsync(accessToken, storedToken.UserId, "User logout");
			}

			storedToken.RevokedAt = DateTime.Now;
			await _supabaseService.UpdateAsync(storedToken);
		}
	}
}


