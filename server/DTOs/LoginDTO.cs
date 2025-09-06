using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class LoginDTO
    {
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }
}
