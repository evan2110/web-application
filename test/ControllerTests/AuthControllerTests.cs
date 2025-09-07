using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using server.Controllers;
using server.DTOs;
using server.Models;
using server.Services;
using server.Utilities;

namespace test.controllerTest
{
    public class AuthControllerTests
    {
        private (AuthController controller, Mock<ISupabaseService> supabase, Mock<ITokenService> token, Mock<IMailService> mail, Mock<IBlacklistService> blacklist, IConfiguration configuration) BuildController()
        {
            var supabase = new Mock<ISupabaseService>();
            var token = new Mock<ITokenService>();
            var mail = new Mock<IMailService>();
            var blacklist = new Mock<IBlacklistService>();

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "test-issuer" },
                { "Jwt:Audience", "test-aud" },
                { "Jwt:SecretKey", new string('a', 32) },
                { "Jwt:RefreshTokenExpirationDays", "7" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            var controller = new AuthController(supabase.Object, token.Object, configuration, mail.Object, blacklist.Object);
            return (controller, supabase, token, mail, blacklist, configuration);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenInvalidPayload()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var request = new RegisterDTO { Email = " ", Password = " ", UserType = " " };

            var result = await controller.Register(request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            var (controller, supabase, _, _, _, _) = BuildController();

            var existing = new List<User>
            {
                new User { Id = 1, Email = "john@example.com", Password = "hash", UserType = "user", CreatedAt = DateTime.Now }
            };
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(existing);

            var request = new RegisterDTO { Email = "john@example.com", Password = "123456", UserType = "user" };

            var result = await controller.Register(request);

            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var (controller, supabase, _, _, _, _) = BuildController();

            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User>());
            supabase.Setup(s => s.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 10; return u; });

            var request = new RegisterDTO { Email = "alice@example.com", Password = "123456", UserType = "user" };

            var result = await controller.Register(request);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenMissingFields()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var result = await controller.Login(new LoginDTO { Email = " ", Password = " " });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserNotExist()
        {
            var (controller, supabase, _, _, _, _) = BuildController();
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User>());
            var result = await controller.Login(new LoginDTO { Email = "a@b.com", Password = "123456" });
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_AdminFlow_ReturnsConflict_AndSendsMail()
        {
            var (controller, supabase, _, mail, _, _) = BuildController();
            var users = new List<User> { new User { Id = 2, Email = "admin@site.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = "admin", CreatedAt = DateTime.Now } };
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(users);
            supabase.Setup(s => s.GetAllAsync<UserCodeVerify>()).ReturnsAsync(new List<UserCodeVerify>());
            supabase.Setup(s => s.CreateAsync(It.IsAny<UserCodeVerify>())).ReturnsAsync((UserCodeVerify x) => { x.Id = 1; return x; });

            var result = await controller.Login(new LoginDTO { Email = "admin@site.com", Password = "123456" });

            result.Should().BeOfType<ConflictObjectResult>();
            mail.Verify(m => m.SendAsync(It.IsAny<MailDataReqDTO>()), Times.Once);
        }

        [Fact]
        public async Task Login_UserFlow_ReturnsOk_WithTokens()
        {
            var (controller, supabase, token, _, _, _) = BuildController();
            var users = new List<User> { new User { Id = 3, Email = "user@site.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = "user", CreatedAt = DateTime.Now } };
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(users);
            token.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("access-token");
            token.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            supabase.Setup(s => s.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => { rt.Id = 1; return rt; });

            var result = await controller.Login(new LoginDTO { Email = "user@site.com", Password = "123456" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Refresh_ReturnsBadRequest_WhenMissingToken()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var result = await controller.Refresh(new RefreshDTO { RefreshToken = "  " });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenTokenInvalid()
        {
            var (controller, supabase, token, _, _, configuration) = BuildController();
            supabase.Setup(s => s.GetAllAsync<RefreshToken>()).ReturnsAsync(new List<RefreshToken>());
            var result = await controller.Refresh(new RefreshDTO { RefreshToken = "bad" });
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Refresh_ReturnsOk_WhenValid()
        {
            var (controller, supabase, token, _, _, configuration) = BuildController();
            var existingToken = new RefreshToken { Id = 1, Token = "old", UserId = 9, ExpiresAt = DateTime.Now.AddDays(1) };
            supabase.Setup(s => s.GetAllAsync<RefreshToken>()).ReturnsAsync(new List<RefreshToken> { existingToken });
            supabase.Setup(s => s.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);
            supabase.Setup(s => s.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => { rt.Id = 2; return rt; });
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User> { new User { Id = 9, Email = "x@y.com", Password = "h", UserType = "user", CreatedAt = DateTime.Now } });
            token.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("new-access");
            token.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

            var result = await controller.Refresh(new RefreshDTO { RefreshToken = "old" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Logout_ReturnsBadRequest_WhenMissingTokens()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var result = await controller.Logout(new LogoutDTO { AccessToken = " ", RefreshToken = " " });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Logout_ReturnsNotFound_WhenRefreshTokenNotFound()
        {
            var (controller, supabase, _, _, _, _) = BuildController();
            supabase.Setup(s => s.GetAllAsync<RefreshToken>()).ReturnsAsync(new List<RefreshToken>());
            var result = await controller.Logout(new LogoutDTO { AccessToken = "acc", RefreshToken = "ref" });
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Logout_ReturnsOk_WhenSuccess()
        {
            var (controller, supabase, _, _, blacklist, _) = BuildController();
            var stored = new RefreshToken { Id = 5, Token = "ref", UserId = 2, ExpiresAt = DateTime.Now.AddDays(1), RevokedAt = null };
            supabase.Setup(s => s.GetAllAsync<RefreshToken>()).ReturnsAsync(new List<RefreshToken> { stored });
            supabase.Setup(s => s.UpdateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);
            blacklist.Setup(b => b.AddTokenToBlacklistAsync("acc", 2, It.IsAny<string>())).ReturnsAsync(true);

            var result = await controller.Logout(new LogoutDTO { AccessToken = "acc", RefreshToken = "ref" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Verify_ReturnsBadRequest_WhenInvalid()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var result = await controller.VerifyUser(new VerifyUserDTO { Email = " ", UserCodeVerify = " " });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Verify_ReturnsUnauthorized_WhenUserNotFound()
        {
            var (controller, supabase, _, _, _, _) = BuildController();
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User>());
            var result = await controller.VerifyUser(new VerifyUserDTO { Email = "x@y.com", UserCodeVerify = "123456" });
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Verify_ReturnsOk_WhenCodeMatches()
        {
            var (controller, supabase, token, _, _, _) = BuildController();
            var user = new User { Id = 7, Email = "u@y.com", Password = "h", UserType = "user", CreatedAt = DateTime.Now };
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User> { user });
            supabase.Setup(s => s.GetAllAsync<UserCodeVerify>()).ReturnsAsync(new List<UserCodeVerify> { new UserCodeVerify { Id = 1, UserId = 7, VerifyCode = "654321", Status = 1 } });
            token.Setup(t => t.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("ac");
            token.Setup(t => t.GenerateRefreshToken()).Returns("rf");
            supabase.Setup(s => s.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => { rt.Id = 11; return rt; });

            var result = await controller.VerifyUser(new VerifyUserDTO { Email = "u@y.com", UserCodeVerify = "654321" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task SendMail_ReturnsBadRequest_WhenInvalidEmail()
        {
            var (controller, _, _, _, _, _) = BuildController();
            var result = await controller.SendMail("");
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SendMail_ReturnsUnauthorized_WhenUserNotFound()
        {
            var (controller, supabase, _, _, _, _) = BuildController();
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User>());
            var result = await controller.SendMail("x@y.com");
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task SendMail_ReturnsOk_WhenSuccess()
        {
            var (controller, supabase, _, mail, _, _) = BuildController();
            var user = new User { Id = 8, Email = "m@y.com", Password = "h", UserType = "user", CreatedAt = DateTime.Now };
            supabase.Setup(s => s.GetAllAsync<User>()).ReturnsAsync(new List<User> { user });
            supabase.Setup(s => s.GetAllAsync<UserCodeVerify>()).ReturnsAsync(new List<UserCodeVerify> { new UserCodeVerify { Id = 2, UserId = 8, VerifyCode = "111111", Status = 1 } });
            supabase.Setup(s => s.UpdateAsync(It.IsAny<UserCodeVerify>())).ReturnsAsync((UserCodeVerify v) => v);

            var result = await controller.SendMail("m@y.com");

            result.Should().BeOfType<OkObjectResult>();
            mail.Verify(m => m.SendAsync(It.IsAny<MailDataReqDTO>()), Times.Once);
        }
    }
}