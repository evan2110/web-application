using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using server.Controllers;
using server.Models;
using server.Services;
using server.Utilities;

namespace test.controllerTest
{
    public class UsersControllerTests
    {
        private (UsersController controller, Mock<ISupabaseService> supabase, Mock<IMessageProvider> messages) Build()
        {
            var supabase = new Mock<ISupabaseService>();
            var logger = new Mock<ILogger<UsersController>>();
            var messages = new Mock<IMessageProvider>();
            messages.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((string code, string? d) => code);

            var controller = new UsersController(supabase.Object, logger.Object, messages.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            return (controller, supabase, messages);
        }

        [Fact]
        public async Task GetUsers_Ok_OnSuccess()
        {
            var (controller, supabase, _) = Build();
            supabase.Setup(s => s.GetAllAsync<User>())
                .ReturnsAsync(new[]
                {
                    new User{ Id = 1, Email = "a@b.com" },
                    new User{ Id = 2, Email = "c@d.com" }
                });

            var result = await controller.GetUsers();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetUsers_500_OnException()
        {
            var (controller, supabase, _) = Build();
            supabase.Setup(s => s.GetAllAsync<User>())
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.GetUsers();
            (result as ObjectResult)!.StatusCode.Should().Be(500);
        }
    }
}


