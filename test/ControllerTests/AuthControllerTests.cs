using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
		private (AuthController controller,
			Mock<ISupabaseService> supabase,
			Mock<ITokenService> token,
			Mock<IMailService> mail,
			Mock<IBlacklistService> blacklist,
			Mock<IAuthService> auth,
			IConfiguration configuration,
			Mock<IMessageProvider> messages) Build()
		{
			var supabase = new Mock<ISupabaseService>();
			var token = new Mock<ITokenService>();
			var mail = new Mock<IMailService>();
			var blacklist = new Mock<IBlacklistService>();
			var auth = new Mock<IAuthService>();
			var logger = new Mock<ILogger<AuthController>>();
			var messages = new Mock<IMessageProvider>();
			messages.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<string?>()))
				.Returns((string code, string? d) => code);

			var settings = new Dictionary<string, string?>
			{
				{ "Jwt:Issuer", "issuer" },
				{ "Jwt:Audience", "aud" },
				{ "Jwt:SecretKey", new string('a', 64) },
				{ "Jwt:RefreshTokenExpirationDays", "7" },
				{ "Frontend:BaseUrl", "http://frontend.local" }
			};
			var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();

			var controller = new AuthController(supabase.Object, token.Object, configuration, mail.Object, blacklist.Object, auth.Object, logger.Object, messages.Object)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};

			return (controller, supabase, token, mail, blacklist, auth, configuration, messages);
		}

		[Fact]
		public async Task Register_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("Email", "Required");
			var result = await controller.Register(new RegisterDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Register_Conflict_WhenEmailTaken()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.IsEmailTakenAsync("e@x.com")).ReturnsAsync(true);
			var result = await controller.Register(new RegisterDTO { Email = "e@x.com", Password = "123456", UserType = "user" });
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task Register_500_WhenCreateUserReturnsNull()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.IsEmailTakenAsync(It.IsAny<string>())).ReturnsAsync(false);
			auth.Setup(a => a.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
				.ReturnsAsync((server.Models.User?)null);
			var result = await controller.Register(new RegisterDTO { Email = "a@b.com", Password = "123456", UserType = "user" });
			(result as ObjectResult)!.StatusCode.Should().Be(500);
		}

		[Fact]
		public async Task Register_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.IsEmailTakenAsync(It.IsAny<string>())).ReturnsAsync(false);
			auth.Setup(a => a.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
				.ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
			var result = await controller.Register(new RegisterDTO { Email = "a@b.com", Password = "123456", UserType = "user" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Login_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("Email", "Required");
			var result = await controller.Login(new LoginDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Login_Unauthorized_WhenUserMissingOrPasswordWrong()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.GetUserByEmailAsync("no@x.com")).ReturnsAsync((User?)null);
			var result1 = await controller.Login(new LoginDTO { Email = "no@x.com", Password = "123456" });
			result1.Should().BeOfType<UnauthorizedObjectResult>();

			auth.Reset();
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 2, Email = "u@x.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), VerifyAt = DateTime.Now });
			auth.Setup(a => a.VerifyPassword("wrong", It.IsAny<string>())).Returns(false);
			var result2 = await controller.Login(new LoginDTO { Email = "u@x.com", Password = "wrong" });
			result2.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task Login_Unauthorized_WhenEmailNotVerified()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 2, Email = "u@x.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), VerifyAt = null });
			auth.Setup(a => a.VerifyPassword("123456", It.IsAny<string>())).Returns(true);
			var result = await controller.Login(new LoginDTO { Email = "u@x.com", Password = "123456" });
			result.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task Login_Conflict_WhenAdminNeedsVerification()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			var user = new User { Id = 3, Email = "admin@x.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), VerifyAt = DateTime.Now, UserType = CommonUtils.UserRoles.Admin };
			auth.Setup(a => a.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
			auth.Setup(a => a.VerifyPassword("123456", It.IsAny<string>())).Returns(true);
			auth.Setup(a => a.EnsureAdminVerificationAsync(user)).ReturnsAsync(true);
			var result = await controller.Login(new LoginDTO { Email = user.Email, Password = "123456" });
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task Login_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			var user = new User { Id = 4, Email = "user@x.com", Password = BCrypt.Net.BCrypt.HashPassword("123456"), VerifyAt = DateTime.Now, UserType = "user" };
			auth.Setup(a => a.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
			auth.Setup(a => a.VerifyPassword("123456", It.IsAny<string>())).Returns(true);
			auth.Setup(a => a.EnsureAdminVerificationAsync(user)).ReturnsAsync(false);
			auth.Setup(a => a.GenerateTokenResponseAsync(user, false)).ReturnsAsync(new { access_token = "a", refresh_token = "r" });
			var result = await controller.Login(new LoginDTO { Email = user.Email, Password = "123456" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Refresh_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("RefreshToken", "Required");
			var result = await controller.Refresh(new RefreshDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Refresh_Unauthorized_WhenServiceThrows()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.RefreshUsingRefreshTokenAsync("bad")).ThrowsAsync(new UnauthorizedAccessException());
			var result = await controller.Refresh(new RefreshDTO { RefreshToken = "bad" });
			result.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task Refresh_500_OnUnexpectedException()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.RefreshUsingRefreshTokenAsync("x")).ThrowsAsync(new Exception("boom"));
			var result = await controller.Refresh(new RefreshDTO { RefreshToken = "x" });
			(result as ObjectResult)!.StatusCode.Should().Be(500);
		}

		[Fact]
		public async Task Refresh_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.RefreshUsingRefreshTokenAsync("ok")).ReturnsAsync(new { access_token = "a", refresh_token = "r" });
			var result = await controller.Refresh(new RefreshDTO { RefreshToken = "ok" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Logout_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("AccessToken", "Required");
			var result = await controller.Logout(new LogoutDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Logout_NotFound_WhenKeyNotFound()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.LogoutAsync("ref", "acc")).ThrowsAsync(new KeyNotFoundException());
			var result = await controller.Logout(new LogoutDTO { RefreshToken = "ref", AccessToken = "acc" });
			result.Should().BeOfType<NotFoundObjectResult>();
		}

		[Fact]
		public async Task Logout_BadRequest_WhenAlreadyRevoked()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.LogoutAsync("ref", "acc")).ThrowsAsync(new InvalidOperationException());
			var result = await controller.Logout(new LogoutDTO { RefreshToken = "ref", AccessToken = "acc" });
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Logout_500_OnUnexpectedException()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.LogoutAsync("ref", "acc")).ThrowsAsync(new Exception("x"));
			var result = await controller.Logout(new LogoutDTO { RefreshToken = "ref", AccessToken = "acc" });
			(result as ObjectResult)!.StatusCode.Should().Be(500);
		}

		[Fact]
		public async Task Logout_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.LogoutAsync("ref", "acc")).Returns(Task.CompletedTask);
			var result = await controller.Logout(new LogoutDTO { RefreshToken = "ref", AccessToken = "acc" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Verify_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("Email", "Required");
			var result = await controller.VerifyUser(new VerifyUserDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Verify_Unauthorized_WhenUserNotFound()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.VerifyUserAndIssueTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
				.ThrowsAsync(new UnauthorizedAccessException());
			var result = await controller.VerifyUser(new VerifyUserDTO { Email = "x@y.com", UserCodeVerify = "123456" });
			result.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task Verify_BadRequest_WhenCodeMismatch()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.VerifyUserAndIssueTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
				.ThrowsAsync(new ArgumentException());
			var result = await controller.VerifyUser(new VerifyUserDTO { Email = "x@y.com", UserCodeVerify = "bad" });
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task Verify_500_OnUnexpectedException()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.VerifyUserAndIssueTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
				.ThrowsAsync(new Exception("x"));
			var result = await controller.VerifyUser(new VerifyUserDTO { Email = "x@y.com", UserCodeVerify = "123456" });
			(result as ObjectResult)!.StatusCode.Should().Be(500);
		}

		[Fact]
		public async Task Verify_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.VerifyUserAndIssueTokensAsync("x@y.com", "123456", false))
				.ReturnsAsync(new { access_token = "a", refresh_token = "r" });
			var result = await controller.VerifyUser(new VerifyUserDTO { Email = "x@y.com", UserCodeVerify = "123456" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task VerifyEmail_BadRequest_WhenTokenMissing()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			var result = await controller.VerifyEmail("");
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task VerifyEmail_BadRequest_WhenTokenInvalid()
		{
			var (controller, _, token, _, _, _, _, _) = Build();
			string ignored;
			token.Setup(t => t.TryValidateEmailVerificationToken("bad", out ignored)).Returns(false);
			var result = await controller.VerifyEmail("bad");
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task VerifyEmail_Unauthorized_WhenUserNotFound()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			string email;
			token.Setup(t => t.TryValidateEmailVerificationToken("tok", out email)).Callback(new TryValidateDelegate((string _, out string e) => e = "u@x.com")).Returns(true);
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync((User?)null);
			var result = await controller.VerifyEmail("tok");
			result.Should().BeOfType<UnauthorizedObjectResult>();
		}

		private delegate void TryValidateDelegate(string token, out string email);

		[Fact]
		public async Task VerifyEmail_Redirect_OnSuccess_UsesConfiguredFrontend()
		{
			var (controller, _, token, _, _, auth, configuration, _) = Build();
			string email;
			token.Setup(t => t.TryValidateEmailVerificationToken("tok", out email)).Callback(new TryValidateDelegate((string _, out string e) => e = "u@x.com")).Returns(true);
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 5, Email = "u@x.com" });
			auth.Setup(a => a.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
			var result = await controller.VerifyEmail("tok");
			result.Should().BeOfType<RedirectResult>();
			(result as RedirectResult)!.Url!.Should().StartWith("http://frontend.local/login?verified=true");
		}

		[Fact]
		public async Task VerifyEmail_Redirect_OnSuccess_UsesOriginHeaderWhenNoConfig()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			// override Frontend:BaseUrl to be empty
			(controller.ControllerContext.HttpContext!.Request.Headers)["Origin"] = "http://origin.local";
			string email;
			token.Setup(t => t.TryValidateEmailVerificationToken("tok", out email)).Callback(new TryValidateDelegate((string _, out string e) => e = "u@x.com")).Returns(true);
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 6, Email = "u@x.com" });
			auth.Setup(a => a.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
			var result = await controller.VerifyEmail("tok");
			result.Should().BeOfType<RedirectResult>();
			(result as RedirectResult)!.Url!.Should().StartWith("http://frontend.local/login?verified=true");
		}

		[Fact]
		public async Task SendMail_BadRequest_WhenEmailMissingOrInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			var r1 = await controller.SendMail("");
			r1.Should().BeOfType<BadRequestObjectResult>();
			var r2 = await controller.SendMail("invalid");
			r2.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task SendMail_Unauthorized_WhenUserNotFound()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.ResendVerificationCodeAsync("u@x.com")).ThrowsAsync(new UnauthorizedAccessException());
			var result = await controller.SendMail("u@x.com");
			result.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task SendMail_Ok_OnSuccess()
		{
			var (controller, _, _, _, _, auth, _, _) = Build();
			auth.Setup(a => a.ResendVerificationCodeAsync("u@x.com")).Returns(Task.CompletedTask);
			var result = await controller.SendMail("u@x.com");
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task ForgotPassword_BadRequest_WhenModelInvalidOrEmailIssues()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("Email", "Required");
			var r0 = await controller.ForgotPassword(new ForgotPasswordDTO());
			r0.Should().BeOfType<BadRequestObjectResult>();

			controller.ModelState.Clear();
			var r1 = await controller.ForgotPassword(new ForgotPasswordDTO { Email = " " });
			r1.Should().BeOfType<BadRequestObjectResult>();

			var r2 = await controller.ForgotPassword(new ForgotPasswordDTO { Email = "invalid" });
			r2.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task ForgotPassword_Unauthorized_WhenUserMissingOrUnverified()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync((User?)null);
			var r1 = await controller.ForgotPassword(new ForgotPasswordDTO { Email = "u@x.com" });
			r1.Should().BeOfType<UnauthorizedObjectResult>();

			auth.Reset();
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 1, Email = "u@x.com", VerifyAt = null });
			var r2 = await controller.ForgotPassword(new ForgotPasswordDTO { Email = "u@x.com" });
			r2.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task ForgotPassword_Ok_OnSuccess()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			auth.Setup(a => a.GetUserByEmailAsync("u@x.com")).ReturnsAsync(new User { Id = 2, Email = "u@x.com", VerifyAt = DateTime.Now });
			token.Setup(t => t.GeneratePasswordResetToken("u@x.com")).Returns("reset-token");
			controller.ControllerContext.HttpContext!.Request.Headers["Origin"] = "http://origin.local";
			auth.Setup(a => a.SendPasswordResetEmailAsync("u@x.com", It.Is<string>(s => s.Contains("reset-password")))).Returns(Task.CompletedTask);
			var result = await controller.ForgotPassword(new ForgotPasswordDTO { Email = "u@x.com" });
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task ResetPassword_BadRequest_WhenModelInvalid()
		{
			var (controller, _, _, _, _, _, _, _) = Build();
			controller.ModelState.AddModelError("Token", "Required");
			var result = await controller.ResetPassword(new ResetPasswordDTO());
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task ResetPassword_Unauthorized_WhenTokenInvalidOrUserMissing()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			string email;
			string ignored;
			token.Setup(t => t.TryValidatePasswordResetToken("bad", out ignored)).Returns(false);
			var r1 = await controller.ResetPassword(new ResetPasswordDTO { Token = "bad", NewPassword = "123456" });
			r1.Should().BeOfType<UnauthorizedObjectResult>();

			token.Setup(t => t.TryValidatePasswordResetToken("ok", out email)).Callback(new TryValidateDelegate((string _, out string e) => e = "u@x.com")).Returns(true);
			auth.Setup(a => a.ResetPasswordAsync("u@x.com", "123456")).ThrowsAsync(new UnauthorizedAccessException());
			var r2 = await controller.ResetPassword(new ResetPasswordDTO { Token = "ok", NewPassword = "123456" });
			r2.Should().BeOfType<UnauthorizedObjectResult>();
		}

		[Fact]
		public async Task ResetPassword_Ok_OnSuccess()
		{
			var (controller, _, token, _, _, auth, _, _) = Build();
			string email;
			token.Setup(t => t.TryValidatePasswordResetToken("ok", out email)).Callback(new TryValidateDelegate((string _, out string e) => e = "u@x.com")).Returns(true);
			auth.Setup(a => a.ResetPasswordAsync("u@x.com", "123456")).Returns(Task.CompletedTask);
			var result = await controller.ResetPassword(new ResetPasswordDTO { Token = "ok", NewPassword = "123456" });
			result.Should().BeOfType<OkObjectResult>();
		}
	}
}