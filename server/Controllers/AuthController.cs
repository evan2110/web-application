using Microsoft.AspNetCore.Mvc;
using server.DTOs;
using server.Models;
using server.Services;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISupabaseService _supabaseService;

        public AuthController(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            var users = await _supabaseService.GetAllAsync<User>();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                CreatedAt = u.CreatedAt,
                Password = u.Password,
                Email = u.Email,
                UserType = u.UserType
            });

            return Ok(userDtos);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser()
        {
            try
            {
                var newUser = new User
                {
                    Id = 0,
                    Password = "TEST",
                    Email = "TEST",
                    UserType = "ADMIN",
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _supabaseService.CreateAsync(newUser);
                if (createdUser != null)
                {
                    var userDto = new UserDto
                    {
                        Id = createdUser.Id,
                        CreatedAt = createdUser.CreatedAt
                    };
                    return Ok(userDto);
                }

                return BadRequest("Failed to create user");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

    }
}
