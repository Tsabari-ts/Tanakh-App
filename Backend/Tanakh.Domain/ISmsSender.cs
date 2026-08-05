using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain.Entities;

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
        // type is written to sms_log by the implementation itself (see
        // Sms4FreeSmsSender) - every call is logged regardless of caller,
        // so callers don't each need their own logging code.
        Task<SmsSendResult> SendAsync(
            string phoneNumberE164, string message, SmsMessageType type, CancellationToken cancellationToken = default);
    }
}
