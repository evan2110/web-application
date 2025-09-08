using System.Text;
using System.Text.RegularExpressions;

namespace server.Utilities
{
    public static class CommonUtils
    {
        public static class UserRoles
        {
            public const string Admin = "admin";
        }
        public static class MessageCodes
        {
            public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
            public const string FailedCreateUser = "FAILED_CREATE_USER";
            public const string InternalServerError = "INTERNAL_SERVER_ERROR";
            public const string InvalidEmailOrPassword = "INVALID_EMAIL_OR_PASSWORD";
            public const string PleaseAuthenticateLogin = "PLEASE_AUTHENTICATE_LOGIN";
            public const string RefreshTokenRequired = "REFRESH_TOKEN_REQUIRED";
            public const string InvalidOrExpiredRefreshToken = "INVALID_OR_EXPIRED_REFRESH_TOKEN";
            public const string UserNotFound = "USER_NOT_FOUND";
            public const string RefreshTokenNotFound = "REFRESH_TOKEN_NOT_FOUND";
            public const string RefreshTokenAlreadyRevoked = "REFRESH_TOKEN_ALREADY_REVOKED";
            public const string VerifyCodeRequired = "VERIFY_CODE_REQUIRED";
            public const string VerifyCodeNotMatching = "VERIFY_CODE_NOT_MATCHING";
            public const string EmailRequired = "EMAIL_REQUIRED";
            public const string EmailWrongFormat = "EMAIL_WRONG_FORMAT";
            public const string VerifyCodeSent = "VERIFY_CODE_SENT";
            public const string LoggedOutSucessfully = "LOGGED_OUT_SUCCESSFULLY";
            public const string EmailNotVerify = "EMAIL_NOT_VERIFY";
            public const string PleaseVerifyEmailBeforeLogin = "PLEASE_VERIFY_EMAIL_BEFORE_LOGIN";
            public const string InvalidVerificationLink = "INVALID_VERIFICATION_LINK";
            public const string InvalidOrExpiredVerificationLink = "INVALID_OR_EXPIRED_VERIFICATION_LINK";
            public const string EmailVerifiedSuccessfully = "EMAIL_VERIFIED_SUCCESSFULLY";
        }
        public static string GenerateVerificationCode()
        {
            var codeLength = 6;
            var characters = "123456789";
            var codeBuilder = new StringBuilder();

            var random = new Random();
            for (var i = 0; i < codeLength; i++)
            {
                var index = random.Next(characters.Length);
                codeBuilder.Append(characters[index]);
            }

            return codeBuilder.ToString();
        }

        public static bool IsValidEmail(string email)
        {
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(email, emailPattern);
        }
    }

    public interface IMessageProvider
    {
        string Get(string code, string? defaultMessage = null);
    }

    public class MessageProvider : IMessageProvider
    {
        private readonly IConfiguration _configuration;

        public MessageProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Get(string code, string? defaultMessage = null)
        {
            var value = _configuration[$"Messages:{code}"];
            return string.IsNullOrWhiteSpace(value) ? (defaultMessage ?? code) : value;
        }
    }
}
