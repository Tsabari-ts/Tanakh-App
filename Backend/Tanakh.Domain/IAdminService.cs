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

    public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int Limit);

    // Current/Previous are counts over equal-length windows; ChangePercent
    // is null when Previous is 0 and Current isn't (an undefined "infinite"
    // percent change - the frontend shows "new" instead of a number).
    public record KpiValue(int Current, int Previous, double? ChangePercent);

    public record AdminOverview(
        KpiValue TotalUsers,
        KpiValue SignupsInRange,
        KpiValue ActiveUsers,
        SmsBalanceResult SmsBalance,
        KpiValue SmsSentInRange,
        int OpenErrors);

    public record UserListItem(
        Guid Id,
        string? PhoneNumber,
        string? DisplayName,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public record SmsLogItem(
        Guid Id,
        string MaskedPhone,
        string Type,
        bool Success,
        int? ProviderStatusCode,
        DateTimeOffset CreatedAt);

    public record SmsStats(
        int SentToday,
        int SentThisWeek,
        int SentThisMonth,
        double SuccessRatePercent,
        IReadOnlyDictionary<string, int> CountByType);

    public record ErrorLogItem(
        Guid Id,
        string Level,
        string Message,
        string? StackTrace,
        string? Endpoint,
        int? StatusCode,
        bool Resolved,
        DateTimeOffset CreatedAt);

    public record TopError(string Message, int Count);

    public interface IAdminService
    {
        Task<AdminDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);

        // Returns false if there was nothing matching to act on (unknown/
        // invalid phone, or delivery id not in a state the action applies
        // to) - a diagnosable no-op, not an error.
        Task<bool> UnsubscribeByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

        Task<bool> RequeueFailedDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);

        Task<AdminOverview> GetOverviewAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<PagedResult<UserListItem>> GetUsersAsync(
            string? search, string? status, DateTimeOffset? from, DateTimeOffset? to,
            int page, int limit, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserListItem>> ExportUsersAsync(
            string? search, string? status, DateTimeOffset? from, DateTimeOffset? to,
            CancellationToken cancellationToken = default);

        // All three return false if the subscriber id doesn't exist -
        // same "diagnosable no-op" convention as UnsubscribeByPhoneAsync.
        Task<bool> BlockUserAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> UnblockUserAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PagedResult<SmsLogItem>> GetSmsLogAsync(
            bool? success, DateTimeOffset? from, DateTimeOffset? to,
            int page, int limit, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SmsLogItem>> ExportSmsLogAsync(
            bool? success, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);

        Task<SmsStats> GetSmsStatsAsync(CancellationToken cancellationToken = default);

        Task SendTestSmsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<ErrorLogItem>> GetErrorLogAsync(
            IReadOnlyList<string>? levels, DateTimeOffset? from, DateTimeOffset? to, string? search,
            int page, int limit, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ErrorLogItem>> ExportErrorLogAsync(
            IReadOnlyList<string>? levels, DateTimeOffset? from, DateTimeOffset? to, string? search,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TopError>> GetTopErrorsAsync(CancellationToken cancellationToken = default);

        Task<bool> ResolveErrorAsync(Guid id, CancellationToken cancellationToken = default);

        Task<int> CleanupErrorLogAsync(int olderThanDays, CancellationToken cancellationToken = default);
    }
}
