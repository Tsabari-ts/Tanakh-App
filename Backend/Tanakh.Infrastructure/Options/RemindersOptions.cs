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

        public int ConfirmTokenTtlHours { get; set; } = 48;

        public int ManageTokenTtlDays { get; set; } = 90;

        // Frontend origin - confirmation/unsubscribe pages link back here
        // (e.g. "start reading" -> a chapter URL) once they're done.
        public string PublicBaseUrl { get; set; } = string.Empty;

        // This API's own origin - confirmation/unsubscribe links in emails
        // point here, since those pages are server-rendered (not part of
        // the Angular app).
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
