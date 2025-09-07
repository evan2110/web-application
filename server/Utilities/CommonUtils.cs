using System.Text;

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
    }
}
