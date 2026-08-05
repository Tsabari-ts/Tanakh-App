namespace Tanakh.Infrastructure.Options
{
    public class AdminOptions
    {
        public const string SectionName = "Admin";

        public string Username { get; set; } = string.Empty;

        // Output of `dotnet run -- --hash-admin-password <pw>` - never a
        // plaintext password. See AdminPasswordHasher.
        public string PasswordHash { get; set; } = string.Empty;

        // E.164, where login OTP codes are sent.
        public string Phone { get; set; } = string.Empty;

        // SMS4FREE balance threshold below which the dashboard shows a
        // low-balance warning (Phase 2) - lives here now to avoid a second
        // options-class migration later.
        public int LowBalanceThreshold { get; set; } = 50;
    }
}
