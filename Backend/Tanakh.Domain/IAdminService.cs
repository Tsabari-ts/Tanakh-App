using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public record FailedDeliverySummary(Guid DeliveryId, string? PhoneNumber, string? LastError, int AttemptCount);

    public record ScheduledDeliverySummary(Guid DeliveryId, string? PhoneNumber, DateTimeOffset ScheduledFor, string Status);

    public record AdminDashboard(
        int ActiveSubscribers,
        int UnsubscribedSubscribers,
        int SignupsLast7Days,
        int FailedDeliveriesLast7Days,
        IReadOnlyList<FailedDeliverySummary> RecentFailedDeliveries,
        IReadOnlyList<ScheduledDeliverySummary> DeliveriesScheduledToday);

    public interface IAdminService
    {
        Task<AdminDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);

        // Returns false if there was nothing matching to act on (unknown/
        // invalid phone, or delivery id not in a state the action applies
        // to) - a diagnosable no-op, not an error.
        Task<bool> UnsubscribeByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

        Task<bool> RequeueFailedDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);
    }
}
