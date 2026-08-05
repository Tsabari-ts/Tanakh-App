namespace Tanakh.Api.Model
{
    public class AdminLoginRequest
    {
        public required string Username { get; set; }

        public required string Password { get; set; }
    }
}
