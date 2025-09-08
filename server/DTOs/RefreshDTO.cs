using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class RefreshDTO
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; }
    }
}
