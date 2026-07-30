using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class JewishCalendarController : ControllerBase
    {
        private readonly IJewishCalendarService jewishCalendarService;

        public JewishCalendarController(IJewishCalendarService jewishCalendarService)
        {
            this.jewishCalendarService = jewishCalendarService;
        }

        /// <summary>Returns whether today falls between candle lighting and Havdalah, per hebcal.com.</summary>
        [HttpGet]
        [Route("getJewishCalendar")]
        public async Task<IActionResult> GetJewishCalendarAsync(CancellationToken cancellationToken)
        {
            return Ok(await jewishCalendarService.IsBetweenCandleLightingAndHavdalahAsync(cancellationToken));
        }
    }
}
