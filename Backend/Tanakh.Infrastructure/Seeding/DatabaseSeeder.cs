using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Seeding
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly AppDbContext dbContext;
        private readonly IConfiguration configuration;
        private readonly IHashingService hashingService;

        public DatabaseSeeder(AppDbContext dbContext, IConfiguration configuration, IHashingService hashingService)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
            this.hashingService = hashingService;
        }

        public async Task ResetSchemaAsync(CancellationToken cancellationToken = default)
        {
            string connectionString = configuration.GetConnectionString("MigrationsDb")
                ?? throw new InvalidOperationException("Missing required connection string 'ConnectionStrings:MigrationsDb'.");

            DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.UseSnakeCaseNamingConvention();

            await using AppDbContext migrationsContext = new(optionsBuilder.Options);
            IMigrator migrator = migrationsContext.GetInfrastructure().GetRequiredService<IMigrator>();

            // "0" is EF Core's convention for "no migrations applied" -
            // rolls every Down back in reverse, emptying the schema.
            await migrator.MigrateAsync("0", cancellationToken);
            await migrator.MigrateAsync(cancellationToken: cancellationToken);
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (await dbContext.Subscribers.AnyAsync(cancellationToken))
            {
                return;
            }

            Subscriber pending = NewSubscriber("pending@example.com", SubscriberStatus.PendingConfirmation);

            Subscriber active = NewSubscriber("active@example.com", SubscriberStatus.Active);
            active.ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-30);

            Subscriber unsubscribed = NewSubscriber("unsubscribed@example.com", SubscriberStatus.Unsubscribed);
            unsubscribed.UnsubscribedAt = DateTimeOffset.UtcNow.AddDays(-10);
            unsubscribed.UnsubscribeReason = "No longer interested";

            Subscriber bounced = NewSubscriber("bounced@example.com", SubscriberStatus.Bounced);
            Subscriber complained = NewSubscriber("complained@example.com", SubscriberStatus.Complained);

            await dbContext.Subscribers.AddRangeAsync(
                [pending, active, unsubscribed, bounced, complained], cancellationToken);

            await dbContext.ReadingProgresses.AddRangeAsync(
            [
                new ReadingProgress
                {
                    Id = Guid.CreateVersion7(), SubscriberId = active.Id, Section = ReadingSection.Torah,
                    Book = "Genesis", Chapter = 12, Verse = 5, UpdatedAt = DateTimeOffset.UtcNow
                },
                new ReadingProgress
                {
                    Id = Guid.CreateVersion7(), SubscriberId = active.Id, Section = ReadingSection.Neviim,
                    Book = "Joshua", Chapter = 3, UpdatedAt = DateTimeOffset.UtcNow
                }
            ], cancellationToken);

            DateTimeOffset yesterday = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset today = DateTimeOffset.UtcNow;

            await dbContext.ReminderDeliveries.AddRangeAsync(
            [
                new ReminderDelivery
                {
                    Id = Guid.CreateVersion7(), SubscriberId = active.Id, ScheduledFor = yesterday,
                    SentAt = yesterday, Status = DeliveryStatus.Sent, AttemptCount = 1,
                    IdempotencyKey = ReminderDelivery.ComputeIdempotencyKey(active.Id, yesterday)
                },
                new ReminderDelivery
                {
                    Id = Guid.CreateVersion7(), SubscriberId = active.Id, ScheduledFor = today,
                    Status = DeliveryStatus.Pending, AttemptCount = 0,
                    IdempotencyKey = ReminderDelivery.ComputeIdempotencyKey(active.Id, today)
                },
                new ReminderDelivery
                {
                    Id = Guid.CreateVersion7(), SubscriberId = bounced.Id, ScheduledFor = yesterday,
                    Status = DeliveryStatus.Failed, AttemptCount = 3, LastError = "Mailbox does not exist",
                    IdempotencyKey = ReminderDelivery.ComputeIdempotencyKey(bounced.Id, yesterday)
                }
            ], cancellationToken);

            await dbContext.EmailEvents.AddRangeAsync(
            [
                new EmailEvent
                {
                    Id = Guid.CreateVersion7(), Provider = "ses", ProviderEventId = "seed-evt-delivered",
                    EventType = EmailEventType.Delivered, SubscriberId = active.Id, Payload = "{}",
                    OccurredAt = yesterday, ReceivedAt = yesterday
                },
                new EmailEvent
                {
                    Id = Guid.CreateVersion7(), Provider = "ses", ProviderEventId = "seed-evt-bounce",
                    EventType = EmailEventType.Bounce, BounceType = BounceType.Hard, SubscriberId = bounced.Id,
                    Payload = "{}", OccurredAt = yesterday, ReceivedAt = yesterday
                },
                new EmailEvent
                {
                    Id = Guid.CreateVersion7(), Provider = "ses", ProviderEventId = "seed-evt-complaint",
                    EventType = EmailEventType.Complaint, SubscriberId = complained.Id, Payload = "{}",
                    OccurredAt = yesterday, ReceivedAt = yesterday
                }
            ], cancellationToken);

            await dbContext.SuppressionEntries.AddRangeAsync(
            [
                new SuppressionEntry
                {
                    Id = Guid.CreateVersion7(), EmailHash = hashingService.HashEmail(bounced.Email),
                    Reason = SuppressionReason.HardBounce, Source = "seed-data"
                },
                new SuppressionEntry
                {
                    Id = Guid.CreateVersion7(), EmailHash = hashingService.HashEmail(complained.Email),
                    Reason = SuppressionReason.Complaint, Source = "seed-data"
                }
            ], cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static Subscriber NewSubscriber(string email, SubscriberStatus status) => new()
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PreferredTime = new TimeOnly(8, 0),
            Timezone = "Asia/Jerusalem",
            Status = status,
            Locale = "he-IL"
        };
    }
}
