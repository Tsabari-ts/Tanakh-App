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
using Tanakh.Domain.Entities;
using Tanakh.Domain.Scheduling;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Reminders
{
    // Materializes tomorrow's (practically: the next occurrence of each
    // active subscriber's preferred_time) reminder_deliveries row. Runs once
    // at startup (recovers from downtime) and then daily at PlannerCron.
    // Safe to run concurrently across API instances or re-run mid-day: the
    // insert is a raw ON CONFLICT (idempotency_key) DO NOTHING, not a
    // check-then-insert, so it can't race with itself.
    public class ReminderPlannerService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly RemindersOptions options;
        private readonly ILogger<ReminderPlannerService> logger;

        public ReminderPlannerService(
            IServiceScopeFactory scopeFactory,
            IOptions<RemindersOptions> options,
            ILogger<ReminderPlannerService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunPlanningCycleAsync(stoppingToken);

            TimeOnly plannerLocalTime = ParseDailyCronTime(options.PlannerCron);
            TimeZoneInfo plannerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.DefaultTimezone);

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTimeOffset nextRun = ComputeNextRun(plannerLocalTime, plannerTimeZone);
                TimeSpan delay = nextRun - DateTimeOffset.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                await RunPlanningCycleAsync(stoppingToken);
            }
        }

        private async Task RunPlanningCycleAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<Subscriber> activeSubscribers = await dbContext.Subscribers
                .Where(s => s.Status == SubscriberStatus.Active
                    && (s.PausedUntil == null || s.PausedUntil < DateTimeOffset.UtcNow))
                .ToListAsync(cancellationToken);

            int inserted = 0;

            foreach (Subscriber subscriber in activeSubscribers)
            {
                TimeZoneInfo subscriberTimeZone;
                try
                {
                    subscriberTimeZone = TimeZoneInfo.FindSystemTimeZoneById(subscriber.Timezone);
                }
                catch (TimeZoneNotFoundException)
                {
                    logger.LogWarning(
                        "Skipping subscriber {SubscriberId}: unknown timezone '{Timezone}'.",
                        subscriber.Id, subscriber.Timezone);
                    continue;
                }

                // NextOccurrenceResolver already returns a UTC-offset value -
                // Npgsql only accepts UTC-offset DateTimeOffset values for
                // timestamptz parameters, and the DB always round-trips
                // scheduled_for as UTC anyway.
                DateTimeOffset scheduledFor = NextOccurrenceResolver.ComputeNext(
                    DateTimeOffset.UtcNow, subscriber.PreferredTime, subscriberTimeZone);

                string idempotencyKey = ReminderDelivery.ComputeIdempotencyKey(subscriber.Id, scheduledFor);
                Guid id = Guid.CreateVersion7();

                int rows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO reminder_deliveries
                        (id, subscriber_id, scheduled_for, status, attempt_count, idempotency_key, created_at, updated_at)
                       VALUES
                        ({id}, {subscriber.Id}, {scheduledFor}, 'pending', 0, {idempotencyKey}, now(), now())
                       ON CONFLICT (idempotency_key) DO NOTHING",
                    cancellationToken);

                inserted += rows;
            }

            logger.LogInformation(
                "Reminder planner: inserted {Inserted} new delivery row(s) for {Total} active subscriber(s).",
                inserted, activeSubscribers.Count);
        }

        private static TimeOnly ParseDailyCronTime(string cron)
        {
            string[] parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[2] != "*" || parts[3] != "*" || parts[4] != "*")
            {
                throw new InvalidOperationException(
                    $"Unsupported Reminders:PlannerCron '{cron}' - only a fixed daily 'minute hour * * *' form is supported.");
            }

            return new TimeOnly(int.Parse(parts[1]), int.Parse(parts[0]));
        }

        private static DateTimeOffset ComputeNextRun(TimeOnly localTime, TimeZoneInfo timeZone) =>
            NextOccurrenceResolver.ComputeNext(DateTimeOffset.UtcNow, localTime, timeZone);
    }
}
