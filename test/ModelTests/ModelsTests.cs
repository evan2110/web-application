using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
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
        public void User_Has_Table_And_Columns()
        {
            var table = (TableAttribute?)Attribute.GetCustomAttribute(typeof(User), typeof(TableAttribute));
            table!.Name.Should().Be("user");
            typeof(User).GetProperty("Id")!.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Should().NotBeEmpty();
            typeof(User).GetProperty("Email")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
            typeof(User).GetProperty("Password")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
            typeof(User).GetProperty("UserType")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
        }

        [Fact]
        public void RefreshToken_SetGet_Works()
        {
            var rt = new RefreshToken { Token = "t", UserId = 1 };
            rt.Token.Should().Be("t");
            rt.UserId.Should().Be(1);
        }

        [Fact]
        public void RefreshToken_Has_Table_And_Columns()
        {
            var table = (TableAttribute?)Attribute.GetCustomAttribute(typeof(RefreshToken), typeof(TableAttribute));
            table!.Name.Should().Be("refresh_token");
            typeof(RefreshToken).GetProperty("Id")!.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Should().NotBeEmpty();
            typeof(RefreshToken).GetProperty("Token")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
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
        public void UserCodeVerify_Has_Table_And_Columns()
        {
            var table = (TableAttribute?)Attribute.GetCustomAttribute(typeof(UserCodeVerify), typeof(TableAttribute));
            table!.Name.Should().Be("user_code_verify");
            typeof(UserCodeVerify).GetProperty("Id")!.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Should().NotBeEmpty();
            typeof(UserCodeVerify).GetProperty("VerifyCode")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
        }

        [Fact]
        public void BlacklistedToken_SetGet_Works()
        {
            var b = new BlacklistedToken { Token = "abc", Reason = "logout" };
            b.Token.Should().Be("abc");
            b.Reason.Should().Be("logout");
        }

        [Fact]
        public void BlacklistedToken_Has_Table_And_Columns()
        {
            var table = (TableAttribute?)Attribute.GetCustomAttribute(typeof(BlacklistedToken), typeof(TableAttribute));
            table!.Name.Should().Be("blacklisted_token");
            typeof(BlacklistedToken).GetProperty("Id")!.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Should().NotBeEmpty();
            typeof(BlacklistedToken).GetProperty("Token")!.GetCustomAttributes(typeof(ColumnAttribute), false).Should().NotBeEmpty();
        }
    }
}

