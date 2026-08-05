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
using Tanakh.Domain.Sms;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Infrastructure.Reminders
{
    // Polls reminder_deliveries every DispatchIntervalSeconds, claims due
    // rows via SELECT...FOR UPDATE SKIP LOCKED (safe with N concurrent API
    // instances), and sends each through ISmsSender. Folds in the
    // sending-reaper, retry backoff, lateness skip, and send-rate pacing -
    // they all live in the same send loop. The outbox/claim mechanism is
    // unchanged from the email-era design (see docs/adr/001-scheduler.md) -
    // only what happens at the "send" step changed.
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
            ISmsSender smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
            IJewishCalendarService jewishCalendarService = scope.ServiceProvider.GetRequiredService<IJewishCalendarService>();

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
                DeliveryOutcome outcome = await ProcessDeliveryAsync(delivery, isShabbatOrHoliday, dbContext, smsSender, cancellationToken);

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
            ISmsSender smsSender,
            CancellationToken cancellationToken)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == delivery.SubscriberId, cancellationToken);

            if (subscriber is null || subscriber.Status != SubscriberStatus.Active || subscriber.PhoneNumber is null)
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            // Grace window - don't send a badly overdue reminder late.
            if (DateTimeOffset.UtcNow - delivery.ScheduledFor > TimeSpan.FromMinutes(options.MaxLatenessMinutes))
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            // Hard block, unconditional - not gated on
            // subscriber.SkipShabbatHolidays. Blocked deliveries are skipped
            // outright, never queued for after Shabbat/Yom Tov.
            if (isShabbatOrHoliday)
            {
                delivery.Status = DeliveryStatus.Skipped;
                return DeliveryOutcome.Skipped;
            }

            string message = BuildReminderSms(subscriber);
            SmsSegmentCalculator.Result segments = SmsSegmentCalculator.Calculate(message);

            delivery.TargetUrl = options.PublicBaseUrl;
            delivery.MessageBody = message;
            delivery.SegmentCount = segments.SegmentCount;

            SmsSendResult result = await smsSender.SendAsync(subscriber.PhoneNumber, message, cancellationToken);

            delivery.ProviderResponse = result.RawResponse;
            delivery.ProviderStatusCode = result.StatusCode;

            if (result.Success)
            {
                delivery.Status = DeliveryStatus.Sent;
                delivery.SentAt = DateTimeOffset.UtcNow;
                return DeliveryOutcome.Sent;
            }

            // Permanent failures (bad number, rejected content) are never
            // retried - retrying the same request produces the same result.
            if (result.IsPermanentFailure)
            {
                delivery.Status = DeliveryStatus.Failed;
                delivery.LastError = $"SMS4FREE status {result.StatusCode} (permanent).";
                return DeliveryOutcome.Failed;
            }

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
                delivery.LastError = $"SMS4FREE status {result.StatusCode}.";
                return DeliveryOutcome.RetryScheduled;
            }

            delivery.Status = DeliveryStatus.Failed;
            delivery.LastError = $"SMS send failed after all retry attempts (last status {result.StatusCode}).";
            return DeliveryOutcome.Failed;
        }

        private string BuildReminderSms(Subscriber subscriber)
        {
            string nameSegment = string.IsNullOrWhiteSpace(subscriber.DisplayName) ? string.Empty : $" {subscriber.DisplayName}";

            return options.SmsTemplate
                .Replace("{שם}", nameSegment)
                .Replace("{קישור}", options.PublicBaseUrl);
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
