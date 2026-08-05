namespace Tanakh.Domain.Entities
{
    // Stored as text + CHECK constraint, not a native Postgres enum - see
    // docs/database.md for why. Snake-cased in the database via
    // SnakeCaseEnumConverter (Tanakh.Infrastructure).
    //
    // No PendingConfirmation/Bounced/Complained - those existed only for the
    // email double opt-in / bounce-webhook flow, both retired when the
    // reminder channel moved to SMS (no verification step, no bounce
    // webhook from SMS4FREE).
    public enum SubscriberStatus
    {
        Active,
        Unsubscribed
    }
}
