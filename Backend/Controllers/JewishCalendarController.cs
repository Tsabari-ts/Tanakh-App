using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tanakh.Model;

namespace Tanakh.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class JewishCalendarController : ControllerBase
    {
        [HttpGet]
        [Route("getJewishCalendar")]
        public IActionResult GetJewishCalendar()
        {
            bool isBetweenCandleLightingAndHavdalah = false;
            JewishCalendarContainer jewishCalendar = new JewishCalendarContainer();
            jewishCalendar = FillJewishCalendar().GetAwaiter().GetResult();

            DateTime currentDay = new DateTime(2024, 01, 30);
            //DateTime currentDay = DateTime.Now.Date;

            Item todayObject = jewishCalendar.items.FirstOrDefault(obj =>
            {
                DateTime objDate = obj.date.Date;
                return objDate == currentDay && (obj.category == "candles" || obj.category == "havdalah");
            });

            if (todayObject != null)
            {
                bool containsCandles = todayObject.category.Contains("candles");
                bool containsHavdalah = todayObject.category.Contains("havdalah");

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
            JewishCalendarContainer calendarContainer = JsonConvert.DeserializeObject<JewishCalendarContainer>(json);

            return calendarContainer;
        }
    }
}
