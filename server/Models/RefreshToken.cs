using server.Services;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    [Table("refresh_token")]
    public class RefreshToken : BaseModel, IEntityWithId
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Required]
        [Column("token")]
        public string Token { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
    }
}
