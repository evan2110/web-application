using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Models;
using server.Services;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ISupabaseService _supabaseService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ISupabaseService supabaseService, ILogger<UsersController> logger)
        {
            _supabaseService = supabaseService;
            _logger = logger;
        }

        // Admin-only: list users
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                _logger.LogInformation("Admin requested user list");
                var users = await _supabaseService.GetClient()
                    .From<User>()
                    .Get();

                var models = users.Models?.Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    userType = u.UserType,
                    createdAt = u.CreatedAt,
                    confirmedAt = u.ConfirmedAt
                }) ?? Enumerable.Empty<object>();

                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}


