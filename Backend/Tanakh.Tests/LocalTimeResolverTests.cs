using System;
using Tanakh.Domain.Scheduling;
using Xunit;

namespace Tanakh.Tests
{
    public class LocalTimeResolverTests
    {
        private static readonly TimeZoneInfo Jerusalem = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

        [Fact]
        public void Resolve_WinterDate_UsesStandardTimeOffset()
        {
            DateTimeOffset result = LocalTimeResolver.Resolve(new DateOnly(2026, 1, 15), new TimeOnly(20, 0), Jerusalem);

            Assert.Equal(TimeSpan.FromHours(2), result.Offset);
            Assert.Equal(new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero), result.ToUniversalTime());
        }

        [Fact]
        public void Resolve_SummerDate_UsesDaylightTimeOffset()
        {
            DateTimeOffset result = LocalTimeResolver.Resolve(new DateOnly(2026, 7, 15), new TimeOnly(20, 0), Jerusalem);

            Assert.Equal(TimeSpan.FromHours(3), result.Offset);
            Assert.Equal(new DateTimeOffset(2026, 7, 15, 17, 0, 0, TimeSpan.Zero), result.ToUniversalTime());
        }

        // Israel's spring-forward transitions: 02:00 -> 03:00, so 02:00-02:59
        // does not exist on these dates.
        [Theory]
        [InlineData(2026, 3, 27)]
        [InlineData(2027, 3, 26)]
        public void Resolve_NonExistentLocalTime_AdvancesToFirstValidInstantAfterGap(int year, int month, int day)
        {
            DateTimeOffset result = LocalTimeResolver.Resolve(new DateOnly(year, month, day), new TimeOnly(2, 30), Jerusalem);

            // The gap runs 02:00-02:59; the first valid instant after it is 03:00 IDT.
            DateTimeOffset expected = new DateTimeOffset(new DateTime(year, month, day, 3, 0, 0), TimeSpan.FromHours(3));
            Assert.Equal(expected, result);
        }

        // Israel's fall-back transitions: 02:00 IDT -> 01:00 IST, so 01:00-01:59
        // occurs twice on these dates.
        [Theory]
        [InlineData(2026, 10, 25)]
        [InlineData(2027, 10, 31)]
        public void Resolve_AmbiguousLocalTime_UsesFirstDaylightOccurrence(int year, int month, int day)
        {
            DateTimeOffset result = LocalTimeResolver.Resolve(new DateOnly(year, month, day), new TimeOnly(1, 30), Jerusalem);

            // The first (daylight, IDT, UTC+3) occurrence is chronologically earlier
            // than the second (standard, IST, UTC+2) occurrence of the same wall clock time.
            DateTimeOffset expected = new DateTimeOffset(new DateTime(year, month, day, 1, 30, 0), TimeSpan.FromHours(3));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Resolve_AmbiguousLocalTime_IsEarlierThanTheSecondOccurrence()
        {
            DateTimeOffset firstOccurrence = LocalTimeResolver.Resolve(new DateOnly(2026, 10, 25), new TimeOnly(1, 30), Jerusalem);
            DateTimeOffset secondOccurrenceEquivalent = new DateTimeOffset(new DateTime(2026, 10, 25, 1, 30, 0), TimeSpan.FromHours(2));

            Assert.True(firstOccurrence.ToUniversalTime() < secondOccurrenceEquivalent.ToUniversalTime());
        }
    }
}
