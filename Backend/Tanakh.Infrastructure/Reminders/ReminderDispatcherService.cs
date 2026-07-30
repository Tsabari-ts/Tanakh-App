using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Infrastructure.Reminders
{
    // Polls reminder_deliveries every DispatchIntervalSeconds, claims due
    // rows via SELECT...FOR UPDATE SKIP LOCKED (safe with N concurrent API
    // instances), and sends each through IEmailSender. Folds in T-07's
    // sending-reaper, T-08's retry backoff, T-09's lateness skip, and T-10's
    // send-rate pacing - they all live in the same send loop.
    public class ReminderDispatcherService : BackgroundService
    {
        private static readonly TimeSpan SendingReaperThreshold = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly RemindersOptions options;
        private readonly ILogger<ReminderDispatcherService> logger;

        public ReminderDispatcherService(
            IServiceScopeFactory scopeFactory,
            IOptions<RemindersOptions> options,
            ILogger<ReminderDispatcherService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(options.DispatchIntervalSeconds));

            do
            {
                try
                {
                    await RunDispatchCycleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Reminder dispatcher cycle failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task RunDispatchCycleAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IEmailSender emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            ISuppressionService suppressionService = scope.ServiceProvider.GetRequiredService<ISuppressionService>();
            IHashingService hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();
            INextChapterResolver nextChapterResolver = scope.ServiceProvider.GetRequiredService<INextChapterResolver>();
            IJewishCalendarService jewishCalendarService = scope.ServiceProvider.GetRequiredService<IJewishCalendarService>();
            IUnsubscribeTokenService unsubscribeTokenService = scope.ServiceProvider.GetRequiredService<IUnsubscribeTokenService>();

            await ReapStuckSendingRowsAsync(dbContext, cancellationToken);

            List<Guid> claimedIds = await ClaimDueDeliveriesAsync(dbContext, cancellationToken);
            if (claimedIds.Count == 0)
            {
                return;
            }

            // One calendar check per cycle, not per delivery - every claimed
            // row is being processed at essentially the same real-world
            // instant anyway (checked every DispatchIntervalSeconds).
            bool isShabbatOrHoliday = await jewishCalendarService.IsBlockedAsync(DateTimeOffset.UtcNow, cancellationToken);

            List<ReminderDelivery> deliveries = await dbContext.ReminderDeliveries
                .Where(d => claimedIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            int sent = 0, skipped = 0, failed = 0, retried = 0;

            foreach (ReminderDelivery delivery in deliveries)
            {
                DeliveryOutcome outcome = await ProcessDeliveryAsync(
                    delivery,
                    isShabbatOrHoliday,
                    dbContext,
                    emailSender,
                    suppressionService,
                    hashingService,
                    nextChapterResolver,
                    unsubscribeTokenService,
                    cancellationToken);

                switch (outcome)
                {
                    case DeliveryOutcome.Sent: sent++; break;
                    case DeliveryOutcome.Skipped: skipped++; break;
                    case DeliveryOutcome.Failed: failed++; break;
                    case DeliveryOutcome.RetryScheduled: retried++; break;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                if (outcome == DeliveryOutcome.Sent && options.SendRatePerSecond > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.0 / options.SendRatePerSecond), cancellationToken);
                }
            }

            logger.LogInformation(
                "Reminder dispatcher: sent={Sent} skipped={Skipped} retryScheduled={Retried} failed={Failed} (batch of {Total}).",
                sent, skipped, retried, failed, deliveries.Count);
        }

        private enum DeliveryOutcome { Sent, Skipped, Failed, RetryScheduled }

        private async Task<DeliveryOutcome> ProcessDeliveryAsync(
            ReminderDelivery delivery,
            bool isShabbatOrHoliday,
            AppDbContext dbContext,
            IEmailSender emailSender,
            ISuppressionService suppressionService,
            IHashingService hashingService,
            INextChapterResolver nextChapterResolver,
            IUnsubscribeTokenService unsubscribeTokenService,
            CancellationToken cancellationToken)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == delivery.SubscriberId, cancellationToken);

            if (subscriber is null || subscriber.Status != SubscriberStatus.Active)
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            // T-09: grace window - don't send a badly overdue reminder late.
            if (DateTimeOffset.UtcNow - delivery.ScheduledFor > TimeSpan.FromMinutes(options.MaxLatenessMinutes))
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            // T-12: hard block, unconditional - not gated on
            // subscriber.SkipShabbatHolidays. Blocked deliveries are
            // skipped outright, never queued for after Shabbat/Yom Tov.
            if (isShabbatOrHoliday)
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            // T-11: belt-and-braces - EmailSender itself also checks this,
            // but checking here lets a suppressed address be recorded as
            // Skipped specifically, rather than a generic Failed.
            if (await suppressionService.IsSuppressedAsync(subscriber.Email, cancellationToken))
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            NextChapterResult next = await nextChapterResolver.ResolveAsync(subscriber.Id, cancellationToken);
            string subscriberToken = unsubscribeTokenService.Issue(subscriber.Id);
            // T-22: lets analytics attribute reading sessions back to reminder emails.
            string targetUrl = $"{options.PublicBaseUrl}/books/{next.Section.ToLowerInvariant()}/{next.Book}/{next.Chapter}/false" +
                $"?sid={Uri.EscapeDataString(subscriberToken)}&src=reminder&utm_source=email&utm_medium=reminder&utm_campaign=daily";
            string unsubscribeUrl = $"{options.ApiBaseUrl}/api/v1/subscriptions/unsubscribe?token={Uri.EscapeDataString(subscriberToken)}";

            delivery.TargetUrl = targetUrl;

            EmailMessage message = BuildReminderEmail(subscriber, next, targetUrl, unsubscribeUrl);
            bool sent = await emailSender.SendMessageAsync(message);

            if (sent)
            {
                delivery.Status = DeliveryStatus.Sent;
                delivery.SentAt = DateTimeOffset.UtcNow;
                return DeliveryOutcome.Sent;
            }

            // T-08: retry with backoff up to MaxAttempts, then a final Failed.
            // Raw SMTP gives no reliable transient/permanent signal (that
            // needs a real ESP's structured error codes / bounce webhooks,
            // deliberately out of scope while still on SmtpClient), so every
            // failure is treated as retryable until attempts are exhausted.
            if (delivery.AttemptCount < options.MaxAttempts)
            {
                int backoffMinutes = 1;
                if (options.RetryBackoffMinutes.Length > 0)
                {
                    int backoffIndex = Math.Min(delivery.AttemptCount - 1, options.RetryBackoffMinutes.Length - 1);
                    backoffMinutes = options.RetryBackoffMinutes[backoffIndex];
                }

                delivery.Status = DeliveryStatus.Pending;
                delivery.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(backoffMinutes);
                delivery.LastError = "Email delivery failed.";
                return DeliveryOutcome.RetryScheduled;
            }

            delivery.Status = DeliveryStatus.Failed;
            delivery.LastError = "Email delivery failed after all retry attempts.";
            return DeliveryOutcome.Failed;
        }

        private static EmailMessage BuildReminderEmail(Subscriber subscriber, NextChapterResult next, string targetUrl, string unsubscribeUrl)
        {
            string greeting = string.IsNullOrWhiteSpace(subscriber.DisplayName) ? "שלום" : $"שלום {subscriber.DisplayName}";
            string cycleNote = next.CompletedCycle
                ? "\n\nסיימת מחזור קריאה שלם של התנ\"ך - מתחילים שוב מבראשית!"
                : string.Empty;

            return new EmailMessage
            {
                To = subscriber.Email,
                Subject = "התזכורת היומית שלך לקריאת תנ\"ך",
                Body = $"{greeting},\n\n" +
                       $"הגיע הזמן להמשיך בקריאת התנ\"ך. הפרק של היום:\n{targetUrl}{cycleNote}\n\n" +
                       $"לביטול ההרשמה לתזכורות: {unsubscribeUrl}",
                Headers = new Dictionary<string, string>
                {
                    ["List-Unsubscribe"] = $"<{unsubscribeUrl}>",
                    ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
                }
            };
        }

        private static async Task ReapStuckSendingRowsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE reminder_deliveries
                   SET status = 'pending'
                   WHERE status = 'sending' AND updated_at < now() - {SendingReaperThreshold}",
                cancellationToken);
        }

        private async Task<List<Guid>> ClaimDueDeliveriesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
        {
            return await dbContext.Database.SqlQueryRaw<Guid>(
                @"UPDATE reminder_deliveries d
                  SET status = 'sending', attempt_count = attempt_count + 1, updated_at = now()
                  FROM (
                      SELECT id FROM reminder_deliveries
                      WHERE status = 'pending'
                        AND scheduled_for <= now()
                        AND (next_attempt_at IS NULL OR next_attempt_at <= now())
                      ORDER BY scheduled_for
                      FOR UPDATE SKIP LOCKED
                      LIMIT {0}
                  ) s
                  WHERE d.id = s.id
                  RETURNING d.id",
                options.BatchSize)
                .ToListAsync(cancellationToken);
        }
    }
}
