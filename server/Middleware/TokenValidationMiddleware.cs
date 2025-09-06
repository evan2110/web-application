using server.Services;

namespace server.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _excludedPaths = new[] { "/api/auth/login", "/api/auth/register", "/api/auth/refresh", "/api/auth/logout", "/" };

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

            var isValid = tokenService.ValidateAccessToken(token);
            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Access token invalid or expired");
                return;
            }

            // If value, continue
            await _next(context);
        }
    }
}
