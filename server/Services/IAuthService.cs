using server.Models;

namespace server.Services
{
	public interface IAuthService
	{
		Task<User?> GetUserByEmailAsync(string email);
		Task<bool> IsEmailTakenAsync(string email);
		bool VerifyPassword(string plainText, string hashed);
		Task<User> CreateUserAsync(string email, string password, string? userType);
		Task<bool> EnsureAdminVerificationAsync(User user);
		Task<object> RefreshUsingRefreshTokenAsync(string refreshToken);
		Task LogoutAsync(string refreshToken, string? accessToken);
		Task<object> VerifyUserAndIssueTokensAsync(string email, string verifyCode, bool rememberMe);
		Task ResendVerificationCodeAsync(string email);
		Task<object> GenerateTokenResponseAsync(User user, bool rememberMe = false);
		Task SendVerificationEmailAsync(string toEmail, string code);
		Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
		Task ResetPasswordAsync(string email, string newPassword);
	}
}


