using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Infrastructure.Services
{
    public interface IJewishCalendarService
    {
        Task<bool> IsBetweenCandleLightingAndHavdalahAsync(CancellationToken cancellationToken);

        // Same candle-lighting/Havdalah check as above, but for an arbitrary
        // instant rather than "right now" - lets the reminder dispatcher
        // (T-12) ask "would sending at this delivery's scheduled_for land
        // during Shabbat/a Yom Tov" instead of only being able to ask about
        // the current moment.
        Task<bool> IsBlockedAsync(DateTimeOffset instant, CancellationToken cancellationToken);
    }
}
