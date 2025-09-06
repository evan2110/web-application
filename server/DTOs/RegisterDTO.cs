using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string UserType { get; set; } = null!;
    }
}
