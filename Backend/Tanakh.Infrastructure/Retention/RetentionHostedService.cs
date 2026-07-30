using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Retention
{
    // Amendment 13 requires not retaining data longer than necessary - see
    // docs/privacy.md. Runs on RetentionOptions.RunInterval, deletes/
    // anonymizes in batches to avoid long locks on large tables, and logs
    // every run (table, rows affected, duration).
    public class RetentionHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly RetentionOptions options;
        private readonly ILogger<RetentionHostedService> logger;

        public RetentionHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RetentionOptions> options,
            ILogger<RetentionHostedService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(options.RunInterval);

            do
            {
                try
                {
                    await RunSweepAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Retention sweep failed - will retry on the next scheduled run.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task RunSweepAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ISubscriberAnonymizationService anonymizationService =
                scope.ServiceProvider.GetRequiredService<ISubscriberAnonymizationService>();

            DateTimeOffset reminderDeliveriesCutoff = DateTimeOffset.UtcNow.AddDays(-options.ReminderDeliveriesRetentionDays);
            await RunBatchedDeleteAsync(
                "reminder_deliveries",
                () => dbContext.ReminderDeliveries
                    .Where(d => d.CreatedAt < reminderDeliveriesCutoff)
                    .OrderBy(d => d.Id)
                    .Take(options.BatchSize)
                    .ExecuteDeleteAsync(cancellationToken),
                cancellationToken);

            DateTimeOffset emailEventsCutoff = DateTimeOffset.UtcNow.AddDays(-options.EmailEventsRetentionDays);
            await RunBatchedDeleteAsync(
                "email_events",
                () => dbContext.EmailEvents
                    .Where(e => e.ReceivedAt < emailEventsCutoff)
                    .OrderBy(e => e.Id)
                    .Take(options.BatchSize)
                    .ExecuteDeleteAsync(cancellationToken),
                cancellationToken);

            await AnonymizeExpiredSubscribersAsync(dbContext, anonymizationService, cancellationToken);
        }

        private async Task RunBatchedDeleteAsync(string tableName, Func<Task<int>> deleteBatchAsync, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int totalDeleted = 0;
            int deletedThisBatch;

            do
            {
                deletedThisBatch = await deleteBatchAsync();
                totalDeleted += deletedThisBatch;

                if (deletedThisBatch == options.BatchSize)
                {
                    await Task.Delay(options.DelayBetweenBatches, cancellationToken);
                }
            }
            while (deletedThisBatch == options.BatchSize && !cancellationToken.IsCancellationRequested);

            logger.LogInformation(
                "Retention sweep: deleted {RowCount} rows from {Table} in {ElapsedMilliseconds}ms",
                totalDeleted, tableName, stopwatch.ElapsedMilliseconds);
        }

        private async Task AnonymizeExpiredSubscribersAsync(
            AppDbContext dbContext,
            ISubscriberAnonymizationService anonymizationService,
            CancellationToken cancellationToken)
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMonths(-options.UnsubscribedSubscriberRetentionMonths);
            Stopwatch stopwatch = Stopwatch.StartNew();
            int totalAnonymized = 0;
            int batchCount;

            do
            {
                // "deleted-" prefix marks rows already anonymized, so a
                // previously-anonymized subscriber (still status =
                // unsubscribed, still past the cutoff) isn't reprocessed
                // every sweep.
                List<Guid> batch = await dbContext.Subscribers
                    .Where(s => s.Status == SubscriberStatus.Unsubscribed
                        && s.UnsubscribedAt != null
                        && s.UnsubscribedAt < cutoff
                        && !s.Email.StartsWith("deleted-"))
                    .OrderBy(s => s.Id)
                    .Take(options.BatchSize)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                foreach (Guid subscriberId in batch)
                {
                    await anonymizationService.AnonymizeAsync(subscriberId, cancellationToken);
                }

                totalAnonymized += batch.Count;
                batchCount = batch.Count;

                if (batchCount == options.BatchSize)
                {
                    await Task.Delay(options.DelayBetweenBatches, cancellationToken);
                }
            }
            while (batchCount == options.BatchSize && !cancellationToken.IsCancellationRequested);

            logger.LogInformation(
                "Retention sweep: anonymized {RowCount} unsubscribed subscribers in {ElapsedMilliseconds}ms",
                totalAnonymized, stopwatch.ElapsedMilliseconds);
        }
    }
}
