using System;

namespace Tanakh.Domain.Validation
{
    // Callers that write subscribers.timezone must validate against this
    // before saving - the DB only checks non-empty (see the check
    // constraint in SubscriberConfiguration), not that the value is a real
    // IANA zone.
    public static class TimeZoneValidator
    {
        public static bool IsValidIanaId(string timezone) =>
            !string.IsNullOrWhiteSpace(timezone) && TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    }
}
