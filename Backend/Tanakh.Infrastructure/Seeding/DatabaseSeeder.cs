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

        public DatabaseSeeder(AppDbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
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

            Subscriber active = NewSubscriber("+972501111111", SubscriberStatus.Active);
            active.DisplayName = "דנה";

            Subscriber unsubscribed = NewSubscriber("+972502222222", SubscriberStatus.Unsubscribed);
            unsubscribed.UnsubscribedAt = DateTimeOffset.UtcNow.AddDays(-10);

            Subscriber failing = NewSubscriber("+972503333333", SubscriberStatus.Active);

            await dbContext.Subscribers.AddRangeAsync([active, unsubscribed, failing], cancellationToken);

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
                    MessageBody = "היי דנה 😊 הגיע הזמן לפרק התנ\"ך היומי שלך! לחצו להמשיך לקרוא: https://localhost:4200 לביטול תזכורות - בהגדרות באתר",
                    SegmentCount = 2, ProviderStatusCode = 1, ProviderResponse = "{\"status\":1,\"message\":\"ok\"}",
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
                    Id = Guid.CreateVersion7(), SubscriberId = failing.Id, ScheduledFor = yesterday,
                    Status = DeliveryStatus.Failed, AttemptCount = 3, LastError = "SMS4FREE status -3 (permanent).",
                    ProviderStatusCode = -3, ProviderResponse = "{\"status\":-3,\"message\":\"לא נמצאו נמענים\"}",
                    IdempotencyKey = ReminderDelivery.ComputeIdempotencyKey(failing.Id, yesterday)
                }
            ], cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static Subscriber NewSubscriber(string phoneNumber, SubscriberStatus status) => new()
        {
            Id = Guid.CreateVersion7(),
            PhoneNumber = phoneNumber,
            PreferredTime = new TimeOnly(8, 0),
            Timezone = "Asia/Jerusalem",
            Status = status,
            Locale = "he-IL"
        };
    }
}
