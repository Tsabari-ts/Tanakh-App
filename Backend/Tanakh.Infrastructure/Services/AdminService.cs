using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private const int RecentFailedDeliveriesLimit = 50;

        private readonly AppDbContext dbContext;
        private readonly ISubscriptionService subscriptionService;

        public AdminService(AppDbContext dbContext, ISubscriptionService subscriptionService)
        {
            this.dbContext = dbContext;
            this.subscriptionService = subscriptionService;
        }

        public async Task<AdminDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            int active = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.Active, cancellationToken);
            int pending = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.PendingConfirmation, cancellationToken);
            int unsubscribed = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.Unsubscribed, cancellationToken);
            int bounced = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.Bounced, cancellationToken);
            int totalSubscribers = await dbContext.Subscribers.CountAsync(cancellationToken);
            int confirmedEver = await dbContext.Subscribers.CountAsync(s => s.ConfirmedAt != null, cancellationToken);

            DateTimeOffset sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
            int signupsLast7Days = await dbContext.Subscribers.CountAsync(s => s.CreatedAt >= sevenDaysAgo, cancellationToken);

            double confirmationRate = totalSubscribers == 0 ? 0 : 100.0 * confirmedEver / totalSubscribers;
            double bounceRate = totalSubscribers == 0 ? 0 : 100.0 * bounced / totalSubscribers;

            List<FailedDeliverySummary> failedDeliveries = await (
                from delivery in dbContext.ReminderDeliveries
                join subscriber in dbContext.Subscribers on delivery.SubscriberId equals subscriber.Id
                where delivery.Status == DeliveryStatus.Failed
                orderby delivery.UpdatedAt descending
                select new FailedDeliverySummary(delivery.Id, subscriber.Email, delivery.LastError, delivery.AttemptCount))
                .Take(RecentFailedDeliveriesLimit)
                .ToListAsync(cancellationToken);

            // DateTimeOffset.UtcNow.Date would implicitly convert through
            // the local system's offset, not UTC (the same pitfall as
            // Npgsql's UTC-only DateTimeOffset parameters elsewhere) -
            // construct it explicitly instead.
            DateTimeOffset todayStartUtc = new(DateTime.UtcNow.Date, TimeSpan.Zero);
            DateTimeOffset todayEndUtc = todayStartUtc.AddDays(1);

            List<ScheduledDeliverySummary> scheduledToday = await (
                from delivery in dbContext.ReminderDeliveries
                join subscriber in dbContext.Subscribers on delivery.SubscriberId equals subscriber.Id
                where delivery.ScheduledFor >= todayStartUtc && delivery.ScheduledFor < todayEndUtc
                orderby delivery.ScheduledFor
                select new ScheduledDeliverySummary(delivery.Id, subscriber.Email, delivery.ScheduledFor, delivery.Status.ToString()))
                .ToListAsync(cancellationToken);

            return new AdminDashboard(
                active, pending, unsubscribed, bounced, signupsLast7Days,
                confirmationRate, bounceRate, failedDeliveries, scheduledToday);
        }

        public async Task<bool> UnsubscribeByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

            if (subscriber is null)
            {
                return false;
            }

            await subscriptionService.UnsubscribeAsync(subscriber.Id, cancellationToken);
            return true;
        }

        public async Task<bool> ResendConfirmationByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email && s.Status == SubscriberStatus.PendingConfirmation, cancellationToken);

            if (subscriber is null)
            {
                return false;
            }

            await subscriptionService.ResendConfirmationAsync(subscriber.Id, cancellationToken);
            return true;
        }

        public async Task<bool> RequeueFailedDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
        {
            // ScheduledFor is bumped to now - otherwise the requeued row's
            // original (long-past) schedule would trip the dispatcher's own
            // MaxLatenessMinutes check and get skipped again immediately.
            int rows = await dbContext.ReminderDeliveries
                .Where(d => d.Id == deliveryId && d.Status == DeliveryStatus.Failed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.Status, DeliveryStatus.Pending)
                    .SetProperty(d => d.ScheduledFor, DateTimeOffset.UtcNow)
                    .SetProperty(d => d.AttemptCount, 0)
                    .SetProperty(d => d.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(d => d.LastError, (string?)null),
                    cancellationToken);

            return rows > 0;
        }
    }
}
