using System.ComponentModel.DataAnnotations;

namespace server.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
