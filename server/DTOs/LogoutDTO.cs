using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class LogoutDTO
    {
        [Required(ErrorMessage = "Access token is required.")]
        public string? AccessToken { get; set; }

        [Required(ErrorMessage = "Refresh token is required.")]
        public string? RefreshToken { get; set; }
    }
}
