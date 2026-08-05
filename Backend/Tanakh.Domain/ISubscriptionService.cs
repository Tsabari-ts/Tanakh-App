using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public record SubscriberPreferences(
        string? PhoneNumber,
        string? DisplayName,
        TimeOnly PreferredTime,
        bool SkipShabbatHolidays,
        DateTimeOffset? PausedUntil);

    public interface ISubscriptionService
    {
        // Upserts by phone number and activates immediately - no email
        // confirmation step and no phone OTP (format validation only, per
        // spec decision log). Returns the signed manage token the client
        // stores (localStorage) to reach GetPreferencesAsync/
        // UpdatePreferencesAsync/UnsubscribeAsync later without a login.
        Task<string> SubscribeAsync(
            string phoneNumberE164,
            string? displayName,
            TimeOnly preferredTime,
            string timezone,
            bool skipShabbatHolidays,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        // Idempotent - safe to call twice for the same subscriber.
        Task UnsubscribeAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        // Admin "unblock" - flips Status back to Active without touching
        // preferences/consent (unlike SubscribeAsync, which is the
        // self-service re-subscribe flow and expects a fresh phone/time/
        // consent submission). Idempotent - safe to call on an already-
        // active subscriber. No-op if the subscriber doesn't exist.
        Task ReactivateAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        // Null if the subscriber doesn't exist or isn't active.
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
