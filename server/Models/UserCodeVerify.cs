using server.Services;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    [Table("user_code_verify")]
    public class UserCodeVerify : BaseModel, IEntityWithId
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("verify_code")]
        public string VerifyCode { get; set; }

        [Column("status")]
        public int Status { get; set; }
    }
}
