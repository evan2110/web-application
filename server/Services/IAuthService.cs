using server.Models;

namespace server.Services
{
	public interface IAuthService
	{
		Task<object> GenerateTokenResponseAsync(User user, bool rememberMe = false);
		Task SendVerificationEmailAsync(string toEmail, string code);
	}
}


