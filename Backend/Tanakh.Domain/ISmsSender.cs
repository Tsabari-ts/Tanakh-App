using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    // Outcome of a single SMS4FREE send attempt. StatusCode/RawResponse are
    // always populated (even on a transport-level failure, where StatusCode
    // is 0 and RawResponse carries a synthetic description) so the caller
    // can always log something useful to reminder_deliveries.
    public record SmsSendResult(
        bool Success,
        bool IsPermanentFailure,
        bool IsLowBalance,
        int StatusCode,
        string RawResponse);

    public interface ISmsSender
    {
        Task<SmsSendResult> SendAsync(string phoneNumberE164, string message, CancellationToken cancellationToken = default);
    }
}
