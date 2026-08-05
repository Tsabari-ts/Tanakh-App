namespace Tanakh.Infrastructure.Options
{
    public class HashingOptions
    {
        public const string SectionName = "Hashing";

        // HMAC key for IHashingService (used today for consent_records.ip_hash)
        // and for the unsubscribe/manage token signature (UnsubscribeTokenService).
        // Never rotate without a documented migration plan - rotating it
        // invalidates every existing manage token in circulation.
        public string Pepper { get; set; } = string.Empty;
    }
}
