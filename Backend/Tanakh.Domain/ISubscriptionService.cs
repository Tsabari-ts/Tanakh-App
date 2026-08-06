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
        // Generates and SMSes a 6-digit code for the given phone (invalidating
        // any prior unused code for that phone first) - required before
        // SubscribeAsync will accept a signup for that number, so a phone
        // can no longer be registered without its owner receiving the code.
        Task RequestOtpAsync(string phoneNumberE164, CancellationToken cancellationToken = default);

        // Checks (and, on success, consumes) the latest unused/unexpired
        // code issued to this phone via RequestOtpAsync. Locked once a code
        // has failed MaxOtpAttempts times, mirroring AdminAuthController's
        // admin-login OTP lockout.
        Task<OtpVerificationResult> VerifyOtpAsync(string phoneNumberE164, string code, CancellationToken cancellationToken = default);

        // Upserts by phone number and activates immediately - no email
        // confirmation step. Callers must call VerifyOtpAsync first and only
        // proceed on OtpVerificationResult.Valid - this method does not
        // re-check the OTP itself, trusting the controller already did.
        // Returns the signed manage token the client stores (localStorage)
        // to reach GetPreferencesAsync/UpdatePreferencesAsync/
        // UnsubscribeAsync later without a login.
        Task<string> SubscribeAsync(
            string phoneNumberE164,
            string? displayName,
            TimeOnly preferredTime,
            string timezone,
            bool skipShabbatHolidays,
            string termsVersion,
            string privacyVersion,
            string consentText,
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
