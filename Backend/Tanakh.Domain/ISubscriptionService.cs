using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public enum ConfirmationOutcome
    {
        Confirmed,
        InvalidToken,
        Expired,
        AlreadyUsed
    }

    public record SubscriberPreferences(
        string Email,
        string? DisplayName,
        TimeOnly PreferredTime,
        bool SkipShabbatHolidays,
        DateTimeOffset? PausedUntil);

    public interface ISubscriptionService
    {
        // Always succeeds from the caller's point of view (upsert semantics) -
        // callers must not use exceptions to distinguish "new" from "existing
        // address" here, so as to not leak whether an address is registered.
        Task SubscribeAsync(
            string email,
            string? displayName,
            TimeOnly preferredTime,
            string timezone,
            bool skipShabbatHolidays,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task<ConfirmationOutcome> ConfirmAsync(string rawToken, CancellationToken cancellationToken = default);

        // No-op if the subscriber doesn't exist or isn't in
        // pending_confirmation - a resend only makes sense there.
        Task ResendConfirmationAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        // Idempotent - unsubscribe tokens carry no expiry and may be replayed
        // (e.g. RFC 8058 one-click retries), so calling this twice for the
        // same subscriber must be harmless.
        Task UnsubscribeAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        // Null if the subscriber doesn't exist or isn't active - the
        // preference center has nothing to manage for a pending/unsubscribed
        // address.
        Task<SubscriberPreferences?> GetPreferencesAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        // Null parameters leave that preference unchanged. Changing
        // preferredTime, pausing, or resuming all cancel any currently
        // pending delivery and (unless now paused) immediately replan the
        // next one at the new time, so the change takes effect the same day
        // instead of waiting for tomorrow's planner run.
        Task UpdatePreferencesAsync(
            Guid subscriberId,
            TimeOnly? preferredTime,
            bool? skipShabbatHolidays,
            bool pauseFor30Days,
            bool resume,
            CancellationToken cancellationToken = default);
    }
}
