using FluentAssertions;
using server.Models;

namespace test.ModelTests
{
    public class ModelsTests
    {
        [Fact]
        public void User_Defaults_ShouldBeInitialized()
        {
            var u = new User();
            u.Email.Should().NotBeNull();
            u.Password.Should().NotBeNull();
            u.UserType.Should().NotBeNull();
        }

        [Fact]
        public void RefreshToken_SetGet_Works()
        {
            var rt = new RefreshToken { Token = "t", UserId = 1 };
            rt.Token.Should().Be("t");
            rt.UserId.Should().Be(1);
        }

        [Fact]
        public void UserCodeVerify_SetGet_Works()
        {
            var v = new UserCodeVerify { UserId = 2, VerifyCode = "123456", Status = 1 };
            v.UserId.Should().Be(2);
            v.VerifyCode.Should().Be("123456");
            v.Status.Should().Be(1);
        }

        [Fact]
        public void BlacklistedToken_SetGet_Works()
        {
            var b = new BlacklistedToken { Token = "abc", Reason = "logout" };
            b.Token.Should().Be("abc");
            b.Reason.Should().Be("logout");
        }
    }
}

