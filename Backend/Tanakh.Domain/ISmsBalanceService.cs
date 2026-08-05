using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public record SmsBalanceResult(bool Ok, int? Balance, string? Error);

    public interface ISmsBalanceService
    {
        Task<SmsBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default);
    }
}
