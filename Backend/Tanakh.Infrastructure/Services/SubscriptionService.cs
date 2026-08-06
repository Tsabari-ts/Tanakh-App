using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Domain.Scheduling;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private const string ConsentPolicyVersion = "2026-07-30";
        private const int OtpValidityMinutes = 10;
        private const int MaxOtpAttempts = 3;
        private const int MaxOtpRequestsPerPhonePerHour = 5;

        private readonly AppDbContext dbContext;
        private readonly IHashingService hashingService;
        private readonly IUnsubscribeTokenService unsubscribeTokenService;
        private readonly ISmsSender smsSender;

        public SubscriptionService(
            AppDbContext dbContext,
            IHashingService hashingService,
            IUnsubscribeTokenService unsubscribeTokenService,
            ISmsSender smsSender)
        {
            this.dbContext = dbContext;
            this.hashingService = hashingService;
            this.unsubscribeTokenService = unsubscribeTokenService;
            this.smsSender = smsSender;
        }

        public async Task RequestOtpAsync(string phoneNumberE164, CancellationToken cancellationToken = default)
        {
            int recentCount = await dbContext.SubscriberOtpCodes
                .Where(o => o.PhoneNumber == phoneNumberE164 && o.CreatedAt > DateTimeOffset.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            if (recentCount >= MaxOtpRequestsPerPhonePerHour)
            {
                return;
            }

            // Invalidate any prior unused code for this phone - only one can
            // be live at a time, mirrors AdminAuthController's admin OTP.
            await dbContext.SubscriberOtpCodes
                .Where(o => o.PhoneNumber == phoneNumberE164 && !o.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Used, true), cancellationToken);

            string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            await dbContext.SubscriberOtpCodes.AddAsync(new SubscriberOtpCode
            {
                Id = Guid.CreateVersion7(),
                PhoneNumber = phoneNumberE164,
                CodeHash = hashingService.Hash(code),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpValidityMinutes),
                Attempts = 0,
                Used = false,
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await smsSender.SendAsync(
                phoneNumberE164,
                $"קוד האימות שלך: {code} (בתוקף ל-{OtpValidityMinutes} דקות)",
                SmsMessageType.Otp,
                cancellationToken);
        }

        public async Task<OtpVerificationResult> VerifyOtpAsync(string phoneNumberE164, string code, CancellationToken cancellationToken = default)
        {
            SubscriberOtpCode? otp = await dbContext.SubscriberOtpCodes
                .Where(o => o.PhoneNumber == phoneNumberE164 && !o.Used && o.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp is null)
            {
                return OtpVerificationResult.Invalid;
            }

            string suppliedHash = hashingService.Hash(code);
            bool match = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(suppliedHash), Encoding.UTF8.GetBytes(otp.CodeHash));

            if (!match)
            {
                otp.Attempts++;
                bool locked = otp.Attempts >= MaxOtpAttempts;
                if (locked)
                {
                    otp.Used = true;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                return locked ? OtpVerificationResult.Locked : OtpVerificationResult.Invalid;
            }

            otp.Used = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return OtpVerificationResult.Valid;
        }

        public async Task<string> SubscribeAsync(
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
            CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.PhoneNumber == phoneNumberE164, cancellationToken);

            if (subscriber is null)
            {
                subscriber = new Subscriber
                {
                    Id = Guid.CreateVersion7(),
                    PhoneNumber = phoneNumberE164,
                    Status = SubscriberStatus.Active
                };
                await dbContext.Subscribers.AddAsync(subscriber, cancellationToken);
            }
            else
            {
                // Re-subscribing (whether previously unsubscribed or
                // already active) reactivates and applies whatever
                // preferences were just submitted - no confirmation step to
                // restart, since there's no email/OTP verification here.
                // Clearing PausedUntil too: a stale pause from a previous
                // subscription cycle must never silently carry over - the
                // user just explicitly opted back in, so reminders should
                // resume immediately, not stay quietly paused.
                subscriber.Status = SubscriberStatus.Active;
                subscriber.UnsubscribedAt = null;
                subscriber.PausedUntil = null;
            }

            subscriber.DisplayName = displayName;
            subscriber.PreferredTime = preferredTime;
            subscriber.Timezone = timezone;
            subscriber.SkipShabbatHolidays = skipShabbatHolidays;

            await RecordConsentAsync(subscriber.Id, termsVersion, privacyVersion, consentText, ipAddress, userAgent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return unsubscribeTokenService.Issue(subscriber.Id);
        }

        public async Task UnsubscribeAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

            if (subscriber is null)
            {
                return;
            }

            if (subscriber.Status != SubscriberStatus.Unsubscribed)
            {
                subscriber.Status = SubscriberStatus.Unsubscribed;
                subscriber.UnsubscribedAt = DateTimeOffset.UtcNow;
                subscriber.PausedUntil = null;
            }

            await dbContext.ReminderDeliveries
                .Where(d => d.SubscriberId == subscriberId && d.Status == DeliveryStatus.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, DeliveryStatus.Skipped), cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ReactivateAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

            // An anonymized subscriber (phone_number = NULL, e.g. after an
            // admin "delete") can't be reactivated - ck_subscribers_
            // phone_required_when_active would reject it, and there's no
            // phone number left to send reminders to anyway.
            if (subscriber is null || subscriber.Status == SubscriberStatus.Active || subscriber.PhoneNumber is null)
            {
                return;
            }

            subscriber.Status = SubscriberStatus.Active;
            subscriber.UnsubscribedAt = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<SubscriberPreferences?> GetPreferencesAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

            if (subscriber is null || subscriber.Status != SubscriberStatus.Active)
            {
                return null;
            }

            return new SubscriberPreferences(
                subscriber.PhoneNumber, subscriber.DisplayName, subscriber.PreferredTime,
                subscriber.SkipShabbatHolidays, subscriber.PausedUntil);
        }

        public async Task UpdatePreferencesAsync(
            Guid subscriberId,
            TimeOnly? preferredTime,
            bool? skipShabbatHolidays,
            bool pauseFor30Days,
            bool resume,
            CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

            if (subscriber is null || subscriber.Status != SubscriberStatus.Active)
            {
                return;
            }

            bool schedulingChanged = preferredTime is not null && preferredTime.Value != subscriber.PreferredTime;

            if (schedulingChanged)
            {
                subscriber.PreferredTime = preferredTime!.Value;
            }

            if (skipShabbatHolidays is not null)
            {
                subscriber.SkipShabbatHolidays = skipShabbatHolidays.Value;
            }

            if (pauseFor30Days)
            {
                subscriber.PausedUntil = DateTimeOffset.UtcNow.AddDays(30);
            }
            else if (resume)
            {
                subscriber.PausedUntil = null;
            }

            bool isPausedNow = subscriber.PausedUntil is not null && subscriber.PausedUntil > DateTimeOffset.UtcNow;

            if (schedulingChanged || pauseFor30Days || resume)
            {
                // Cancel whatever was already planned - a stale time or a
                // pause shouldn't still send today.
                await dbContext.ReminderDeliveries
                    .Where(d => d.SubscriberId == subscriberId && d.Status == DeliveryStatus.Pending)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (!isPausedNow && (schedulingChanged || resume))
            {
                // Replan immediately rather than waiting for tomorrow's
                // planner run, so a same-day time change (or resuming) takes
                // effect right away.
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(subscriber.Timezone);
                DateTimeOffset scheduledFor = NextOccurrenceResolver.ComputeNext(DateTimeOffset.UtcNow, subscriber.PreferredTime, timeZone);
                string idempotencyKey = ReminderDelivery.ComputeIdempotencyKey(subscriberId, scheduledFor);

                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO reminder_deliveries
                        (id, subscriber_id, scheduled_for, status, attempt_count, idempotency_key, created_at, updated_at)
                       VALUES
                        ({Guid.CreateVersion7()}, {subscriberId}, {scheduledFor}, 'pending', 0, {idempotencyKey}, now(), now())
                       ON CONFLICT (idempotency_key) DO NOTHING",
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task RecordConsentAsync(
            Guid subscriberId, string termsVersion, string privacyVersion, string consentText,
            string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            await dbContext.ConsentRecords.AddAsync(new ConsentRecord
            {
                Id = Guid.CreateVersion7(),
                SubscriberId = subscriberId,
                ConsentType = ConsentType.Marketing,
                Granted = true,
                GrantedAt = DateTimeOffset.UtcNow,
                IpHash = hashingService.Hash(ipAddress ?? "unknown"),
                UserAgent = userAgent ?? "unknown",
                PolicyVersion = ConsentPolicyVersion,
                TermsVersion = termsVersion,
                PrivacyVersion = privacyVersion,
                ConsentText = consentText
            }, cancellationToken);
        }
    }
}
