using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class JewishCalendarController : ControllerBase
    {
        /// <summary>Returns whether today falls between candle lighting and Havdalah, per hebcal.com.</summary>
        [HttpGet]
        [Route("getJewishCalendar")]
        public IActionResult GetJewishCalendar()
        {
            bool isBetweenCandleLightingAndHavdalah = false;
            JewishCalendarContainer jewishCalendar = FillJewishCalendar().GetAwaiter().GetResult();

            DateTime currentDay = new DateTime(2024, 01, 30);
            //DateTime currentDay = DateTime.Now.Date;

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

                //DateTime currentDay = todayObject.date.Date;

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
                    TimeSpan HavdalahTime = todayObject.date.TimeOfDay;
                    bool isTimeAfterHavdalah = currentTime > HavdalahTime;

                    if (!isTimeAfterHavdalah)
                    {
                        isBetweenCandleLightingAndHavdalah = !isTimeAfterHavdalah;
                    }
                }
            }

            return Ok(isBetweenCandleLightingAndHavdalah);
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
