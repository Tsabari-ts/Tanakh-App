using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public class JewishCalendarService : IJewishCalendarService
    {
        // The hebcal.com response for a given (Gregorian) year doesn't
        // change once fetched - cached for a day at a time (not
        // indefinitely, to still pick up rare upstream corrections) instead
        // of hitting hebcal.com on every call, which used to happen on
        // every visitor's first page load plus every reminder-dispatch
        // cycle with no cache at all.
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
        private static readonly TimeZoneInfo Jerusalem = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

        private readonly HttpClient httpClient;
        private readonly IMemoryCache cache;

        public JewishCalendarService(HttpClient httpClient, IMemoryCache cache)
        {
            this.httpClient = httpClient;
            this.cache = cache;
        }

        public Task<bool> IsBetweenCandleLightingAndHavdalahAsync(CancellationToken cancellationToken) =>
            IsBlockedAsync(DateTimeOffset.Now, cancellationToken);

        public async Task<bool> IsBlockedAsync(DateTimeOffset instant, CancellationToken cancellationToken)
        {
            DateTimeOffset localInstant = TimeZoneInfo.ConvertTime(instant, Jerusalem);
            DateTime localDate = localInstant.Date;

            JewishCalendarContainer jewishCalendar = await FillJewishCalendarAsync(localDate.Year, cancellationToken);

            // items validated non-null in FillJewishCalendarAsync
            Item? todayObject = jewishCalendar.items!.FirstOrDefault(obj =>
                obj.date.Date == localDate && (obj.category == "candles" || obj.category == "havdalah"));

            if (todayObject is null)
            {
                return false;
            }

            TimeSpan currentTime = localInstant.TimeOfDay;
            TimeSpan eventTime = todayObject.date.TimeOfDay;

            if (todayObject.category?.Contains("candles") == true)
            {
                // Blocked from candle-lighting until midnight; the following
                // day's own "havdalah" entry covers the rest of the window.
                return currentTime >= eventTime;
            }

            if (todayObject.category?.Contains("havdalah") == true)
            {
                // Blocked from midnight until Havdalah.
                return currentTime <= eventTime;
            }

            return false;
        }

        private async Task<JewishCalendarContainer> FillJewishCalendarAsync(int year, CancellationToken cancellationToken)
        {
            string cacheKey = $"jewish-calendar-{year}";

            if (cache.TryGetValue(cacheKey, out JewishCalendarContainer? cached) && cached is not null)
            {
                return cached;
            }

            HttpResponseMessage jsonResult = await httpClient.GetAsync(
                $"https://www.hebcal.com/hebcal?v=1&cfg=json&maj=on&min=on&mod=on&nx=on&ss=on&mf=on&c=on&geo=geoname&geonameid=293397&M=on&s=on&year={year}",
                cancellationToken);
            string json = await jsonResult.Content.ReadAsStringAsync(cancellationToken);
            JewishCalendarContainer? calendarContainer = JsonSerializer.Deserialize<JewishCalendarContainer>(json);

            if (calendarContainer?.items is null)
            {
                throw new InvalidOperationException(
                    "Failed to parse hebcal.com calendar response: missing or empty 'items'.");
            }

            // Sized entry - the shared IMemoryCache has a SizeLimit
            // configured (see Program.cs), same reasoning as SmsBalanceService.
            cache.Set(cacheKey, calendarContainer, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                Size = 1
            });

            return calendarContainer;
        }
    }
}
