using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using server.Middleware;
using server.Services;

namespace test.ServiceTests
{
    public class TokenValidationMiddlewareTests
    {
        private static DefaultHttpContext CreateContext(string path, string? bearer = null)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = path;
            if (!string.IsNullOrWhiteSpace(bearer))
            {
                ctx.Request.Headers["Authorization"] = $"Bearer {bearer}";
            }
            ctx.Response.Body = new MemoryStream();
            return ctx;
        }

        [Fact]
        public async Task Allows_Excluded_Paths()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var mw = new TokenValidationMiddleware(next);
            var tokenService = new Mock<ITokenService>();
            var ctx = CreateContext("/api/auth/login");
            await mw.Invoke(ctx, tokenService.Object);
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Returns401_When_Missing_Token()
        {
            RequestDelegate next = _ => Task.CompletedTask;
            var mw = new TokenValidationMiddleware(next);
            var tokenService = new Mock<ITokenService>();
            var ctx = CreateContext("/protected/resource");
            await mw.Invoke(ctx, tokenService.Object);
            ctx.Response.StatusCode.Should().Be(401);
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ctx.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            body.Should().Contain("Access token missing");
        }

        [Fact]
        public async Task Returns401_When_Token_Invalid()
        {
            RequestDelegate next = _ => Task.CompletedTask;
            var mw = new TokenValidationMiddleware(next);
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(s => s.ValidateAccessTokenWithBlacklistAsync("bad")).ReturnsAsync(false);
            var ctx = CreateContext("/protected/resource", "bad");
            await mw.Invoke(ctx, tokenService.Object);
            ctx.Response.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task CallsNext_When_Token_Valid()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var mw = new TokenValidationMiddleware(next);
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(s => s.ValidateAccessTokenWithBlacklistAsync("ok")).ReturnsAsync(true);
            var ctx = CreateContext("/protected/resource", "ok");
            await mw.Invoke(ctx, tokenService.Object);
            nextCalled.Should().BeTrue();
        }
    }
}


