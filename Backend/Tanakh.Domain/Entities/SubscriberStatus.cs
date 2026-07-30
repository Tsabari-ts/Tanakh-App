namespace Tanakh.Domain.Entities
{
    // Stored as text + CHECK constraint, not a native Postgres enum - see
    // docs/database.md for why. Snake-cased in the database via
    // SnakeCaseEnumConverter (Tanakh.Infrastructure).
    public enum SubscriberStatus
    {
        PendingConfirmation,
        Active,
        Unsubscribed,
        Bounced,
        Complained
    }
}
