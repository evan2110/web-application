using server.Services;

namespace server.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _excludedPaths = new[] { "/api/auth/login", "/api/auth/register", "/api/auth/refresh", "/api/auth/logout", "/api/auth/verify", "/api/auth/sendmail" };

        public TokenValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ITokenService tokenService)
        {
            var requestPath = context.Request.Path.Value?.ToLower();
            if (_excludedPaths.Any(path => requestPath == path))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Access token missing");
                return;
            }

            var isValid = await tokenService.ValidateAccessTokenWithBlacklistAsync(token);
            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Access token invalid, expired, or blacklisted");
                return;
            }

            // If valid, continue
            await _next(context);
        }
    }
}
