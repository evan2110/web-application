using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class VerifyUserDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verify code is required.")]
        [MinLength(6, ErrorMessage = "Verify code must be at least 6 characters.")]
        public string UserCodeVerify { get; set; }

        public bool RememberMe { get; set; }
    }
}
