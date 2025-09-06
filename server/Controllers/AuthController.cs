using Microsoft.AspNetCore.Mvc;
using server.DTOs;
using server.Models;
using server.Services;
using System.Data;

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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var users = await _supabaseService.GetAllAsync<User>();
            if (users.Any(e => e.Email == request.Email))
                return Conflict(new { message = "Email is already in use." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Id = 0,
                Email = request.Email,
                Password = passwordHash,
                UserType = request.UserType,
                CreatedAt = DateTime.UtcNow,
            };

            var createdUser = await _supabaseService.CreateAsync(newUser);

            if (createdUser != null)
            {
                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    Password = createdUser.Password,
                    UserType = createdUser.UserType,
                    CreatedAt = createdUser.CreatedAt
                };

                return Ok(userDto);
            }

            return BadRequest("Failed to create user");
        }

    }
}
