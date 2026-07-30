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
        private static readonly TimeZoneInfo Jerusalem = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

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

        private static async Task<JewishCalendarContainer> FillJewishCalendarAsync(int year, CancellationToken cancellationToken)
        {
            HttpClient httpClient = new HttpClient();
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

            return calendarContainer;
        }
    }
}
