using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private const string ConfirmPurpose = "confirm";
        private const string ConsentPolicyVersion = "2026-07-30";

        private readonly AppDbContext dbContext;
        private readonly IHashingService hashingService;
        private readonly IEmailSender emailSender;
        private readonly RemindersOptions options;

        public SubscriptionService(
            AppDbContext dbContext,
            IHashingService hashingService,
            IEmailSender emailSender,
            IOptions<RemindersOptions> options)
        {
            this.dbContext = dbContext;
            this.hashingService = hashingService;
            this.emailSender = emailSender;
            this.options = options.Value;
        }

        public async Task SubscribeAsync(
            string email,
            string? displayName,
            TimeOnly preferredTime,
            string timezone,
            bool skipShabbatHolidays,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            string normalizedEmail = email.Trim();

            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Email == normalizedEmail, cancellationToken);

            if (subscriber is null)
            {
                subscriber = new Subscriber
                {
                    Id = Guid.CreateVersion7(),
                    Email = normalizedEmail,
                    Status = SubscriberStatus.PendingConfirmation
                };
                await dbContext.Subscribers.AddAsync(subscriber, cancellationToken);
            }
            else if (subscriber.Status != SubscriberStatus.Active)
            {
                // Lets someone who never confirmed, or who previously
                // unsubscribed/bounced, restart the double opt-in with
                // whatever preferences they just submitted.
                subscriber.Status = SubscriberStatus.PendingConfirmation;
                subscriber.UnsubscribedAt = null;
                subscriber.UnsubscribeReason = null;
            }
            else
            {
                // Already active: signup on an already-confirmed address is a
                // no-op besides recording the consent attempt below - use the
                // preference center (T-17) to change settings, not signup.
                await RecordConsentAsync(subscriber.Id, ipAddress, userAgent, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            subscriber.DisplayName = displayName;
            subscriber.PreferredTime = preferredTime;
            subscriber.Timezone = timezone;
            subscriber.SkipShabbatHolidays = skipShabbatHolidays;

            await RecordConsentAsync(subscriber.Id, ipAddress, userAgent, cancellationToken);

            string rawToken = GenerateRawToken();
            await dbContext.ConfirmationTokens.AddAsync(new ConfirmationToken
            {
                TokenHash = ConfirmationToken.ComputeTokenHash(rawToken),
                SubscriberId = subscriber.Id,
                Purpose = ConfirmPurpose,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(options.ConfirmTokenTtlHours)
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            string confirmUrl = $"{options.ApiBaseUrl}/api/v1/subscriptions/confirm?token={Uri.EscapeDataString(rawToken)}";
            await emailSender.SendMessageAsync(new EmailMessage
            {
                To = subscriber.Email,
                Subject = "אישור הרשמה לתזכורת יומית",
                Body = $"שלום{(string.IsNullOrWhiteSpace(displayName) ? "" : " " + displayName)},\n\n" +
                       "כדי להשלים את ההרשמה לתזכורת היומית, יש ללחוץ על הקישור הבא:\n" +
                       $"{confirmUrl}\n\n" +
                       "אם לא ביקשת להירשם, אפשר להתעלם מהודעה זו."
            });
        }

        public async Task<ConfirmationOutcome> ConfirmAsync(string rawToken, CancellationToken cancellationToken = default)
        {
            string tokenHash = ConfirmationToken.ComputeTokenHash(rawToken);

            ConfirmationToken? token = await dbContext.ConfirmationTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Purpose == ConfirmPurpose, cancellationToken);

            if (token is null)
            {
                return ConfirmationOutcome.InvalidToken;
            }

            if (token.UsedAt is not null)
            {
                return ConfirmationOutcome.AlreadyUsed;
            }

            if (token.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return ConfirmationOutcome.Expired;
            }

            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == token.SubscriberId, cancellationToken);

            if (subscriber is null)
            {
                return ConfirmationOutcome.InvalidToken;
            }

            token.UsedAt = DateTimeOffset.UtcNow;
            subscriber.Status = SubscriberStatus.Active;
            subscriber.ConfirmedAt = DateTimeOffset.UtcNow;

            string emailHash = hashingService.HashEmail(subscriber.Email);
            SuppressionEntry? suppression = await dbContext.SuppressionEntries
                .FirstOrDefaultAsync(e => e.EmailHash == emailHash && e.Reason == SuppressionReason.Unsubscribe, cancellationToken);

            if (suppression is not null)
            {
                dbContext.SuppressionEntries.Remove(suppression);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ConfirmationOutcome.Confirmed;
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
                subscriber.UnsubscribeReason = "user_requested";
            }

            string emailHash = hashingService.HashEmail(subscriber.Email);
            bool alreadySuppressed = await dbContext.SuppressionEntries
                .AnyAsync(e => e.EmailHash == emailHash, cancellationToken);

            if (!alreadySuppressed)
            {
                await dbContext.SuppressionEntries.AddAsync(new SuppressionEntry
                {
                    Id = Guid.CreateVersion7(),
                    EmailHash = emailHash,
                    Reason = SuppressionReason.Unsubscribe,
                    Source = "unsubscribe-link"
                }, cancellationToken);
            }

            await dbContext.ReminderDeliveries
                .Where(d => d.SubscriberId == subscriberId && d.Status == DeliveryStatus.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Status, DeliveryStatus.Skipped), cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task RecordConsentAsync(Guid subscriberId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
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
                PolicyVersion = ConsentPolicyVersion
            }, cancellationToken);
        }

        private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
