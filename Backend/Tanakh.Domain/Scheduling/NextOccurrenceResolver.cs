using System;

namespace Tanakh.Domain.Scheduling
{
    // Given "now", finds the next UTC instant a daily local preferred time
    // occurs - today if that slot hasn't passed yet, otherwise tomorrow.
    // Shared by the planner (T-05) and the preference center (T-17), which
    // both need to schedule "the next reminder at this local time".
    public static class NextOccurrenceResolver
    {
        public static DateTimeOffset ComputeNext(DateTimeOffset nowUtc, TimeOnly preferredTime, TimeZoneInfo timeZone)
        {
            DateTimeOffset nowLocal = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
            DateOnly localDate = DateOnly.FromDateTime(nowLocal.Date);

            DateTimeOffset candidate = LocalTimeResolver.Resolve(localDate, preferredTime, timeZone).ToUniversalTime();
            if (candidate < nowUtc)
            {
                candidate = LocalTimeResolver.Resolve(localDate.AddDays(1), preferredTime, timeZone).ToUniversalTime();
            }

            return candidate;
        }
    }
}
