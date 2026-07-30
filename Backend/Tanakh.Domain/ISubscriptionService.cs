using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public enum ConfirmationOutcome
    {
        Confirmed,
        InvalidToken,
        Expired,
        AlreadyUsed
    }

    public interface ISubscriptionService
    {
        // Always succeeds from the caller's point of view (upsert semantics) -
        // callers must not use exceptions to distinguish "new" from "existing
        // address" here, so as to not leak whether an address is registered.
        Task SubscribeAsync(
            string email,
            string? displayName,
            TimeOnly preferredTime,
            string timezone,
            bool skipShabbatHolidays,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task<ConfirmationOutcome> ConfirmAsync(string rawToken, CancellationToken cancellationToken = default);
    }
}
