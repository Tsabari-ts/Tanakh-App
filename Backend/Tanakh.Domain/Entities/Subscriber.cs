using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    public class Subscriber : IHasCreatedAt, IHasUpdatedAt
    {
        // UUID v7 - generated in C# via Guid.CreateVersion7(), never
        // gen_random_uuid(). Time-ordered, index-friendly.
        public Guid Id { get; set; }

        public required string Email { get; set; }

        public string? DisplayName { get; set; }

        public TimeOnly PreferredTime { get; set; }

        public string Timezone { get; set; } = "Asia/Jerusalem";

        public bool SkipShabbatHolidays { get; set; } = true;

        public SubscriberStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public DateTimeOffset? UnsubscribedAt { get; set; }

        public string? UnsubscribeReason { get; set; }

        public string Locale { get; set; } = "he-IL";
    }
}
