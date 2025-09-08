using server.Services;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    [Table("user")]
    public class User : BaseModel, IEntityWithId
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Column("user_type")]
        public string? UserType { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

		[Column("confirmed_at")]
		public DateTime? ConfirmedAt { get; set; }

        [Column("confirmed_token")]
        public string? ConfirmedToken { get; set; }

        [Column("reset_token")]
        public string? ResetToken { get; set; }
    }
}
