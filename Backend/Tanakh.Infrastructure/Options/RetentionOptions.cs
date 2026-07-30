using System;

namespace Tanakh.Infrastructure.Options
{
    // Amendment 13 requires not retaining data longer than necessary - see
    // docs/privacy.md. Defaults match the policy's stated windows; override
    // via the "Retention" config section if that policy ever changes.
    public class RetentionOptions
    {
        public const string SectionName = "Retention";

        public int ReminderDeliveriesRetentionDays { get; set; } = 90;

        public int EmailEventsRetentionDays { get; set; } = 180;

        public int UnsubscribedSubscriberRetentionMonths { get; set; } = 12;

        // How often the retention sweep runs, not how long anything is kept.
        public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(24);

        // Rows per DELETE/UPDATE statement, to avoid long locks/bloat on
        // large tables.
        public int BatchSize { get; set; } = 5000;

        public TimeSpan DelayBetweenBatches { get; set; } = TimeSpan.FromMilliseconds(200);
    }
}
