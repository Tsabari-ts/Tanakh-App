using System;

namespace Tanakh.Infrastructure.Options
{
    public class RemindersOptions
    {
        public const string SectionName = "Reminders";

        // Cron expression for ReminderPlannerService (00:05 Israel time by default).
        public string PlannerCron { get; set; } = "5 0 * * *";

        public int DispatchIntervalSeconds { get; set; } = 60;

        public int MaxLatenessMinutes { get; set; } = 60;

        public int BatchSize { get; set; } = 100;

        public int MaxAttempts { get; set; } = 3;

        public int[] RetryBackoffMinutes { get; set; } = { 1, 5, 25 };

        public int SendRatePerSecond { get; set; } = 10;

        public string DefaultTimezone { get; set; } = "Asia/Jerusalem";

        public string DefaultStartBook { get; set; } = "Genesis";

        public int DefaultStartChapter { get; set; } = 1;

        // {שם} -> " {DisplayName}" (with its own leading space) or empty if
        // no display name; {קישור} -> PublicBaseUrl. Kept in config, not
        // hardcoded, so wording can change without a deploy (see spec §9.1).
        public string SmsTemplate { get; set; } =
            "היי{שם} 😊 הגיע הזמן לפרק התנ\"ך היומי שלך! לחצו להמשיך לקרוא: {קישור} לביטול תזכורות - בהגדרות באתר";

        // Frontend origin - the SMS reminder links back here; the client's
        // own localStorage remembers the last-read chapter and continues
        // from there (no server-computed deep link).
        public string PublicBaseUrl { get; set; } = string.Empty;

        // This API's own origin.
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
