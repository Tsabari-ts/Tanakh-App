namespace Tanakh.Infrastructure.Options
{
    public class HashingOptions
    {
        public const string SectionName = "Hashing";

        // HMAC key for IHashingService. Unlike EmailOptions, this has no
        // graceful-degradation path - a missing pepper means suppression
        // checks can't run at all, and sending must fail closed rather than
        // silently skip the check. Never rotate without a documented
        // migration plan: rotating it invalidates every existing
        // suppression_list.email_hash lookup.
        public string Pepper { get; set; } = string.Empty;
    }
}
