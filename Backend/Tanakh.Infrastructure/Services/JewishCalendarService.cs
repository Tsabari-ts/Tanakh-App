using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public class JewishCalendarService : IJewishCalendarService
    {
        public bool IsBetweenCandleLightingAndHavdalah()
        {
            bool isBetweenCandleLightingAndHavdalah = false;
            JewishCalendarContainer jewishCalendar = FillJewishCalendar().GetAwaiter().GetResult();

            DateTime currentDay = DateTime.Now.Date;

            // items validated non-null in FillJewishCalendar
            Item? todayObject = jewishCalendar.items!.FirstOrDefault(obj =>
            {
                DateTime objDate = obj.date.Date;
                return objDate == currentDay && (obj.category == "candles" || obj.category == "havdalah");
            });

            if (todayObject != null)
            {
                bool containsCandles = todayObject.category?.Contains("candles") == true;
                bool containsHavdalah = todayObject.category?.Contains("havdalah") == true;

                TimeSpan currentTime = DateTimeOffset.Now.TimeOfDay;

                if (containsCandles)
                {
                    TimeSpan candlesTime = todayObject.date.TimeOfDay;
                    bool isTimeBeforeCandles = currentTime < candlesTime;

                    if (!isTimeBeforeCandles)
                    {
                        isBetweenCandleLightingAndHavdalah = !isTimeBeforeCandles;
                    }
                }
                else if (containsHavdalah)
                {
                    TimeSpan havdalahTime = todayObject.date.TimeOfDay;
                    bool isTimeAfterHavdalah = currentTime > havdalahTime;

                    if (!isTimeAfterHavdalah)
                    {
                        isBetweenCandleLightingAndHavdalah = !isTimeAfterHavdalah;
                    }
                }
            }

            return isBetweenCandleLightingAndHavdalah;
        }

        private static async Task<JewishCalendarContainer> FillJewishCalendar()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage jsonResult = await httpClient.GetAsync("https://www.hebcal.com/hebcal?v=1&cfg=json&maj=on&min=on&mod=on&nx=on&ss=on&mf=on&c=on&geo=geoname&geonameid=293397&M=on&s=on");
            string json = await jsonResult.Content.ReadAsStringAsync();
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
