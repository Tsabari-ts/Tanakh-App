using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public record FailedDeliverySummary(Guid DeliveryId, string Email, string? LastError, int AttemptCount);

    public record ScheduledDeliverySummary(Guid DeliveryId, string Email, DateTimeOffset ScheduledFor, string Status);

    public record AdminDashboard(
        int ActiveSubscribers,
        int PendingConfirmationSubscribers,
        int UnsubscribedSubscribers,
        int BouncedSubscribers,
        int SignupsLast7Days,
        double ConfirmationRatePercent,
        double BounceRatePercent,
        IReadOnlyList<FailedDeliverySummary> RecentFailedDeliveries,
        IReadOnlyList<ScheduledDeliverySummary> DeliveriesScheduledToday);

    public interface IAdminService
    {
        Task<AdminDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);

        // Each returns false if there was nothing matching to act on (unknown
        // email / delivery id, or the subscriber/delivery wasn't in a state
        // the action applies to) - a diagnosable no-op, not an error.
        Task<bool> UnsubscribeByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> ResendConfirmationByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> RequeueFailedDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    }
}
