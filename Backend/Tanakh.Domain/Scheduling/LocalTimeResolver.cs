using System;

namespace Tanakh.Domain.Scheduling
{
    // Converts a subscriber's local wall-clock preferred time to a UTC instant.
    // Israel alternates IST (UTC+2) / IDT (UTC+3); a fixed offset would send
    // reminders an hour off for half the year, so every conversion goes
    // through TimeZoneInfo against the actual date.
    public static class LocalTimeResolver
    {
        public static DateTimeOffset Resolve(DateOnly localDate, TimeOnly localTime, TimeZoneInfo timeZone)
        {
            DateTime local = localDate.ToDateTime(localTime, DateTimeKind.Unspecified);

            if (timeZone.IsInvalidTime(local))
            {
                // Spring-forward gap: this local time never occurs. Policy:
                // advance to the first valid instant after the gap.
                local = AdvanceToFirstValidInstant(local, timeZone);
                return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
            }

            if (timeZone.IsAmbiguousTime(local))
            {
                // Fall-back overlap: this local time occurs twice. Policy: use
                // the first (daylight) occurrence - the larger UTC offset is
                // the one that lands earlier on the UTC timeline.
                TimeSpan[] offsets = timeZone.GetAmbiguousTimeOffsets(local);
                TimeSpan daylightOffset = offsets[0];
                foreach (TimeSpan candidate in offsets)
                {
                    if (candidate > daylightOffset)
                    {
                        daylightOffset = candidate;
                    }
                }

                return new DateTimeOffset(local, daylightOffset);
            }

            return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
        }

        private static DateTime AdvanceToFirstValidInstant(DateTime local, TimeZoneInfo timeZone)
        {
            DateTime candidate = local;
            DateTime searchLimit = local.AddHours(4);

            while (timeZone.IsInvalidTime(candidate))
            {
                candidate = candidate.AddMinutes(1);
                if (candidate > searchLimit)
                {
                    throw new InvalidOperationException(
                        $"Could not resolve invalid local time {local:O} in zone '{timeZone.Id}' within a 4-hour search window.");
                }
            }

            return candidate;
        }
    }
}
