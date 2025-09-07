using server.Services;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    [Table("blacklisted_token")]
    public class BlacklistedToken : BaseModel, IEntityWithId
    {
        [PrimaryKey("id")]
        public int Id { get; set; }
        
        [Required]
        [Column("token")]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        [Column("blacklisted_at")]
        public DateTime BlacklistedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }
    }
}
