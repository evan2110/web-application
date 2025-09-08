using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Models;
using server.Services;
using server.Utilities;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ISupabaseService _supabaseService;
        private readonly ILogger<UsersController> _logger;
        private readonly IMessageProvider _messages;

        public UsersController(ISupabaseService supabaseService, ILogger<UsersController> logger, IMessageProvider messages)
        {
            _supabaseService = supabaseService;
            _logger = logger;
            _messages = messages;
        }

        // Admin-only: list users
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                _logger.LogInformation("Admin requested user list");
                var users = await _supabaseService.GetAllAsync<User>();

                var models = users.Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    userType = u.UserType,
                    createdAt = u.CreatedAt,
                    confirmedAt = u.ConfirmedAt
                });

                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users");
                return StatusCode(500, new { message = _messages.Get(CommonUtils.MessageCodes.InternalServerError) });
            }
        }
    }
}


