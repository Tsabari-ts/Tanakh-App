namespace Tanakh.Api.Model
{
    public class AdminUserActionRequest
    {
        // "block" | "unblock"
        public required string Action { get; set; }
    }
}
