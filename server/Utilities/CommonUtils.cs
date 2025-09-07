using System.Text;
using System.Text.RegularExpressions;

namespace server.Utilities
{
    public static class CommonUtils
    {
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
}
