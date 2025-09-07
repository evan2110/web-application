namespace server.DTOs
{
    public class VerifyUserDTO
    {
        public string Email { get; set; }
        public string UserCodeVerify { get; set; }
        public bool RememberMe { get; set; }
    }
}
