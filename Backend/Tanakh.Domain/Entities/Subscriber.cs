using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    public class Subscriber : IHasCreatedAt, IHasUpdatedAt
    {
        // UUID v7 - generated in C# via Guid.CreateVersion7(), never
        // gen_random_uuid(). Time-ordered, index-friendly.
        public Guid Id { get; set; }

        // E.164 ("+972501234567"). Nullable only so an anonymized
        // (12-month-post-unsubscribe) subscriber row can null it out while
        // keeping the row - see SubscriberAnonymizationService. Enforced
        // NOT NULL for active subscribers by ck_subscribers_phone_required.
        public string? PhoneNumber { get; set; }

        public string? DisplayName { get; set; }

        public TimeOnly PreferredTime { get; set; }

        public string Timezone { get; set; } = "Asia/Jerusalem";

        public bool SkipShabbatHolidays { get; set; } = true;

        public SubscriberStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? UnsubscribedAt { get; set; }

        // Active subscriber, but temporarily not receiving reminders until
        // this instant (T-17 "pause for a month"). Null means not paused.
        public DateTimeOffset? PausedUntil { get; set; }

        public string Locale { get; set; } = "he-IL";
    }
}
