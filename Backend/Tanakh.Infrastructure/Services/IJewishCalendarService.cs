using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Infrastructure.Services
{
    public interface IJewishCalendarService
    {
        Task<bool> IsBetweenCandleLightingAndHavdalahAsync(CancellationToken cancellationToken);
    }
}
