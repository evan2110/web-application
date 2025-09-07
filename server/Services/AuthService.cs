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

		public AuthService(ISupabaseService supabaseService, ITokenService tokenService, IConfiguration configuration, IMailService mailService)
		{
			_supabaseService = supabaseService;
			_tokenService = tokenService;
			_configuration = configuration;
			_mailService = mailService;
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
	}
}


