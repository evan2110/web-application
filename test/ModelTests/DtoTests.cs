using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using server.DTOs;

namespace test.ModelTests
{
    public class DtoTests
    {
        private static IList<ValidationResult> Validate(object dto)
        {
            var ctx = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, ctx, results, true);
            return results;
        }

        [Fact]
        public void LoginDTO_Validation_Works()
        {
            var invalid = new LoginDTO { Email = "bad", Password = "123" };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new LoginDTO { Email = "a@b.com", Password = "123456", RememberMe = true };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void RegisterDTO_Validation_Works()
        {
            var invalid = new RegisterDTO { Email = "bad", Password = "1", UserType = null! };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new RegisterDTO { Email = "a@b.com", Password = "123456", UserType = "user" };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void VerifyUserDTO_Validation_Works()
        {
            var invalid = new VerifyUserDTO { Email = "bad", UserCodeVerify = "1" };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new VerifyUserDTO { Email = "a@b.com", UserCodeVerify = "123456", RememberMe = true };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void RefreshDTO_Validation_Works()
        {
            var invalid = new RefreshDTO { RefreshToken = null! };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new RefreshDTO { RefreshToken = "token" };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void LogoutDTO_Validation_Works()
        {
            var invalid = new LogoutDTO { AccessToken = null, RefreshToken = null };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new LogoutDTO { AccessToken = "a", RefreshToken = "r" };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void ForgotPasswordDTO_Validation_Works()
        {
            var invalid = new ForgotPasswordDTO { Email = "bad" };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new ForgotPasswordDTO { Email = "a@b.com" };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void ResetPasswordDTO_Validation_Works()
        {
            var invalid = new ResetPasswordDTO { Token = "", NewPassword = "1" };
            Validate(invalid).Should().NotBeEmpty();

            var valid = new ResetPasswordDTO { Token = "t", NewPassword = "123456" };
            Validate(valid).Should().BeEmpty();
        }

        [Fact]
        public void MailDataReqDTO_SetGet_Works()
        {
            var dto = new MailDataReqDTO { ToEmail = "a@b.com", Subject = "S", Body = "<b>Hi</b>" };
            dto.ToEmail.Should().Be("a@b.com");
            dto.Subject.Should().Be("S");
            dto.Body.Should().Be("<b>Hi</b>");
        }

        [Fact]
        public void MailSettingsDTO_SetGet_Works()
        {
            var dto = new MailSettingsDTO { Email = "noreply@x.com", Password = "pwd", Host = "smtp", DisplayName = "App", Port = 25 };
            dto.Email.Should().Be("noreply@x.com");
            dto.Password.Should().Be("pwd");
            dto.Host.Should().Be("smtp");
            dto.DisplayName.Should().Be("App");
            dto.Port.Should().Be(25);
        }
    }
}


