using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Domain.Validation;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private const int RecentFailedDeliveriesLimit = 50;

        private readonly AppDbContext dbContext;
        private readonly ISubscriptionService subscriptionService;
        private readonly ISubscriberAnonymizationService anonymizationService;
        private readonly ISmsBalanceService smsBalanceService;
        private readonly ISmsSender smsSender;
        private readonly AdminOptions adminOptions;

        public AdminService(
            AppDbContext dbContext,
            ISubscriptionService subscriptionService,
            ISubscriberAnonymizationService anonymizationService,
            ISmsBalanceService smsBalanceService,
            ISmsSender smsSender,
            IOptions<AdminOptions> adminOptions)
        {
            this.dbContext = dbContext;
            this.subscriptionService = subscriptionService;
            this.anonymizationService = anonymizationService;
            this.smsBalanceService = smsBalanceService;
            this.smsSender = smsSender;
            this.adminOptions = adminOptions.Value;
        }

        public async Task<AdminDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            int active = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.Active, cancellationToken);
            int unsubscribed = await dbContext.Subscribers.CountAsync(s => s.Status == SubscriberStatus.Unsubscribed, cancellationToken);

            DateTimeOffset sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
            int signupsLast7Days = await dbContext.Subscribers.CountAsync(s => s.CreatedAt >= sevenDaysAgo, cancellationToken);
            int failedLast7Days = await dbContext.ReminderDeliveries
                .CountAsync(d => d.Status == DeliveryStatus.Failed && d.UpdatedAt >= sevenDaysAgo, cancellationToken);

            List<FailedDeliverySummary> failedDeliveries = await (
                from delivery in dbContext.ReminderDeliveries
                join subscriber in dbContext.Subscribers on delivery.SubscriberId equals subscriber.Id
                where delivery.Status == DeliveryStatus.Failed
                orderby delivery.UpdatedAt descending
                select new FailedDeliverySummary(delivery.Id, subscriber.PhoneNumber, delivery.LastError, delivery.AttemptCount))
                .Take(RecentFailedDeliveriesLimit)
                .ToListAsync(cancellationToken);

            // DateTimeOffset.UtcNow.Date would implicitly convert through
            // the local system's offset, not UTC (the same pitfall as
            // Npgsql's UTC-only DateTimeOffset parameters elsewhere) -
            // construct it explicitly instead.
            DateTimeOffset todayStartUtc = new(DateTime.UtcNow.Date, TimeSpan.Zero);
            DateTimeOffset todayEndUtc = todayStartUtc.AddDays(1);

            List<ScheduledDeliverySummary> scheduledToday = await (
                from delivery in dbContext.ReminderDeliveries
                join subscriber in dbContext.Subscribers on delivery.SubscriberId equals subscriber.Id
                where delivery.ScheduledFor >= todayStartUtc && delivery.ScheduledFor < todayEndUtc
                orderby delivery.ScheduledFor
                select new ScheduledDeliverySummary(delivery.Id, subscriber.PhoneNumber, delivery.ScheduledFor, delivery.Status.ToString()))
                .ToListAsync(cancellationToken);

            return new AdminDashboard(
                active, unsubscribed, signupsLast7Days, failedLast7Days, failedDeliveries, scheduledToday);
        }

        public async Task<bool> UnsubscribeByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            if (IsraeliMobilePhoneValidator.Validate(phoneNumber, out string e164) != IsraeliMobilePhoneValidator.ValidationResult.Valid)
            {
                return false;
            }

            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.PhoneNumber == e164, cancellationToken);

            if (subscriber is null)
            {
                return false;
            }

            await subscriptionService.UnsubscribeAsync(subscriber.Id, cancellationToken);
            return true;
        }

        public async Task<bool> RequeueFailedDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
        {
            // ScheduledFor is bumped to now - otherwise the requeued row's
            // original (long-past) schedule would trip the dispatcher's own
            // MaxLatenessMinutes check and get skipped again immediately.
            int rows = await dbContext.ReminderDeliveries
                .Where(d => d.Id == deliveryId && d.Status == DeliveryStatus.Failed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.Status, DeliveryStatus.Pending)
                    .SetProperty(d => d.ScheduledFor, DateTimeOffset.UtcNow)
                    .SetProperty(d => d.AttemptCount, 0)
                    .SetProperty(d => d.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(d => d.LastError, (string?)null),
                    cancellationToken);

            return rows > 0;
        }

        public async Task<AdminOverview> GetOverviewAsync(
            DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            TimeSpan rangeLength = to - from;
            DateTimeOffset previousFrom = from - rangeLength;

            int totalNow = await dbContext.Subscribers.CountAsync(s => s.CreatedAt < to, cancellationToken);
            int totalPrevious = await dbContext.Subscribers.CountAsync(s => s.CreatedAt < from, cancellationToken);

            int signupsCurrent = await dbContext.Subscribers.CountAsync(s => s.CreatedAt >= from && s.CreatedAt < to, cancellationToken);
            int signupsPrevious = await dbContext.Subscribers
                .CountAsync(s => s.CreatedAt >= previousFrom && s.CreatedAt < from, cancellationToken);

            // "Active users" has no login concept to hang off of in this
            // app - the closest real signal is having actually read
            // something (reading_progress touched) in the window.
            int activeCurrent = await dbContext.ReadingProgresses
                .Where(p => p.UpdatedAt >= from && p.UpdatedAt < to)
                .Select(p => p.SubscriberId).Distinct().CountAsync(cancellationToken);
            int activePrevious = await dbContext.ReadingProgresses
                .Where(p => p.UpdatedAt >= previousFrom && p.UpdatedAt < from)
                .Select(p => p.SubscriberId).Distinct().CountAsync(cancellationToken);

            int smsSentCurrent = await dbContext.SmsLogs
                .CountAsync(l => l.Success && l.CreatedAt >= from && l.CreatedAt < to, cancellationToken);
            int smsSentPrevious = await dbContext.SmsLogs
                .CountAsync(l => l.Success && l.CreatedAt >= previousFrom && l.CreatedAt < from, cancellationToken);

            int openErrors = await dbContext.ErrorLogs.CountAsync(e => !e.Resolved, cancellationToken);

            // Balance is a live gauge, not a period metric - no prior-window
            // comparison exists for it (there's no balance history table),
            // so it's reported as-is rather than wrapped in a KpiValue.
            SmsBalanceResult balance = await smsBalanceService.GetBalanceAsync(cancellationToken);

            return new AdminOverview(
                MakeKpi(totalNow, totalPrevious),
                MakeKpi(signupsCurrent, signupsPrevious),
                MakeKpi(activeCurrent, activePrevious),
                balance,
                MakeKpi(smsSentCurrent, smsSentPrevious),
                openErrors);
        }

        public async Task<PagedResult<UserListItem>> GetUsersAsync(
            string? search, string? status, DateTimeOffset? from, DateTimeOffset? to,
            int page, int limit, CancellationToken cancellationToken = default)
        {
            IQueryable<Subscriber> query = BuildUsersQuery(search, status, from, to);

            int totalCount = await query.CountAsync(cancellationToken);

            List<UserListItem> items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(s => new UserListItem(s.Id, s.PhoneNumber, s.DisplayName, s.Status.ToString(), s.CreatedAt, s.UpdatedAt))
                .ToListAsync(cancellationToken);

            return new PagedResult<UserListItem>(items, totalCount, page, limit);
        }

        public async Task<IReadOnlyList<UserListItem>> ExportUsersAsync(
            string? search, string? status, DateTimeOffset? from, DateTimeOffset? to,
            CancellationToken cancellationToken = default)
        {
            return await BuildUsersQuery(search, status, from, to)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new UserListItem(s.Id, s.PhoneNumber, s.DisplayName, s.Status.ToString(), s.CreatedAt, s.UpdatedAt))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> BlockUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!await dbContext.Subscribers.AnyAsync(s => s.Id == id, cancellationToken))
            {
                return false;
            }

            await subscriptionService.UnsubscribeAsync(id, cancellationToken);
            await WriteAuditAsync("admin.user.block", id, cancellationToken);
            return true;
        }

        public async Task<bool> UnblockUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!await dbContext.Subscribers.AnyAsync(s => s.Id == id, cancellationToken))
            {
                return false;
            }

            await subscriptionService.ReactivateAsync(id, cancellationToken);
            await WriteAuditAsync("admin.user.unblock", id, cancellationToken);
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!await dbContext.Subscribers.AnyAsync(s => s.Id == id, cancellationToken))
            {
                return false;
            }

            // "Delete" anonymizes rather than hard-deleting - consent_records
            // deliberately blocks a hard delete (Restrict FK, kept as legal
            // proof of consent per Amendment 13). Same effect as the
            // automatic 12-month retention sweep, just admin-triggered now.
            //
            // Must unsubscribe first - ck_subscribers_phone_required_when_active
            // rejects nulling phone_number while status is still 'active'.
            // The retention sweep never hits this because it only ever
            // anonymizes subscribers that are already Unsubscribed.
            await subscriptionService.UnsubscribeAsync(id, cancellationToken);
            await anonymizationService.AnonymizeAsync(id, cancellationToken);
            await WriteAuditAsync("admin.user.delete", id, cancellationToken);
            return true;
        }

        public async Task<PagedResult<SmsLogItem>> GetSmsLogAsync(
            bool? success, DateTimeOffset? from, DateTimeOffset? to,
            int page, int limit, CancellationToken cancellationToken = default)
        {
            IQueryable<SmsLog> query = BuildSmsLogQuery(success, from, to);

            int totalCount = await query.CountAsync(cancellationToken);

            List<SmsLog> rows = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return new PagedResult<SmsLogItem>(rows.Select(ToSmsLogItem).ToList(), totalCount, page, limit);
        }

        public async Task<IReadOnlyList<SmsLogItem>> ExportSmsLogAsync(
            bool? success, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
        {
            List<SmsLog> rows = await BuildSmsLogQuery(success, from, to)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            return rows.Select(ToSmsLogItem).ToList();
        }

        public async Task<SmsStats> GetSmsStatsAsync(CancellationToken cancellationToken = default)
        {
            DateTimeOffset todayStart = new(DateTime.UtcNow.Date, TimeSpan.Zero);
            DateTimeOffset weekStart = todayStart.AddDays(-7);
            DateTimeOffset monthStart = todayStart.AddDays(-30);

            int sentToday = await dbContext.SmsLogs.CountAsync(l => l.Success && l.CreatedAt >= todayStart, cancellationToken);
            int sentWeek = await dbContext.SmsLogs.CountAsync(l => l.Success && l.CreatedAt >= weekStart, cancellationToken);
            int sentMonth = await dbContext.SmsLogs.CountAsync(l => l.Success && l.CreatedAt >= monthStart, cancellationToken);
            int totalMonth = await dbContext.SmsLogs.CountAsync(l => l.CreatedAt >= monthStart, cancellationToken);

            double successRate = totalMonth == 0 ? 100 : Math.Round(sentMonth / (double)totalMonth * 100, 1);

            var byType = await dbContext.SmsLogs
                .Where(l => l.CreatedAt >= monthStart)
                .GroupBy(l => l.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            Dictionary<string, int> countByType = byType.ToDictionary(x => x.Type.ToString(), x => x.Count);

            return new SmsStats(sentToday, sentWeek, sentMonth, successRate, countByType);
        }

        public async Task SendTestSmsAsync(CancellationToken cancellationToken = default)
        {
            // Always targets the admin's own configured phone - never an
            // arbitrary recipient from the client (spec: "שלח הודעת בדיקה
            // למספר של המנהל").
            await smsSender.SendAsync(
                adminOptions.Phone, "הודעת בדיקה ממערכת הניהול", SmsMessageType.Test, cancellationToken);
        }

        public async Task<PagedResult<ErrorLogItem>> GetErrorLogAsync(
            IReadOnlyList<string>? levels, DateTimeOffset? from, DateTimeOffset? to, string? search,
            int page, int limit, CancellationToken cancellationToken = default)
        {
            IQueryable<ErrorLog> query = BuildErrorLogQuery(levels, from, to, search);

            int totalCount = await query.CountAsync(cancellationToken);

            List<ErrorLogItem> items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(e => new ErrorLogItem(
                    e.Id, e.Level.ToString(), e.Message, e.StackTrace, e.Endpoint, e.StatusCode, e.Resolved, e.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PagedResult<ErrorLogItem>(items, totalCount, page, limit);
        }

        public async Task<IReadOnlyList<ErrorLogItem>> ExportErrorLogAsync(
            IReadOnlyList<string>? levels, DateTimeOffset? from, DateTimeOffset? to, string? search,
            CancellationToken cancellationToken = default)
        {
            return await BuildErrorLogQuery(levels, from, to, search)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ErrorLogItem(
                    e.Id, e.Level.ToString(), e.Message, e.StackTrace, e.Endpoint, e.StatusCode, e.Resolved, e.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TopError>> GetTopErrorsAsync(CancellationToken cancellationToken = default)
        {
            // Grouping straight into the TopError record constructor doesn't
            // translate (Npgsql/EF Core rejects the GroupBy+Count()->record
            // shape) - project to an anonymous type server-side, then
            // materialize the record client-side, same fix as GetSmsStatsAsync's
            // byType breakdown.
            var grouped = await dbContext.ErrorLogs
                .GroupBy(e => e.Message)
                .Select(g => new { Message = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync(cancellationToken);

            return grouped.Select(g => new TopError(g.Message, g.Count)).ToList();
        }

        public async Task<bool> ResolveErrorAsync(Guid id, CancellationToken cancellationToken = default)
        {
            int rows = await dbContext.ErrorLogs
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Resolved, true), cancellationToken);

            return rows > 0;
        }

        public async Task<int> CleanupErrorLogAsync(int olderThanDays, CancellationToken cancellationToken = default)
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);

            return await dbContext.ErrorLogs
                .Where(e => e.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private IQueryable<Subscriber> BuildUsersQuery(string? search, string? status, DateTimeOffset? from, DateTimeOffset? to)
        {
            IQueryable<Subscriber> query = dbContext.Subscribers;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern = $"%{search.Trim()}%";
                query = query.Where(s =>
                    (s.PhoneNumber != null && EF.Functions.ILike(s.PhoneNumber, pattern)) ||
                    (s.DisplayName != null && EF.Functions.ILike(s.DisplayName, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse(status, ignoreCase: true, out SubscriberStatus parsedStatus))
            {
                query = query.Where(s => s.Status == parsedStatus);
            }

            if (from is not null)
            {
                query = query.Where(s => s.CreatedAt >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(s => s.CreatedAt < to.Value);
            }

            return query;
        }

        private IQueryable<SmsLog> BuildSmsLogQuery(bool? success, DateTimeOffset? from, DateTimeOffset? to)
        {
            IQueryable<SmsLog> query = dbContext.SmsLogs;

            if (success is not null)
            {
                query = query.Where(l => l.Success == success.Value);
            }

            if (from is not null)
            {
                query = query.Where(l => l.CreatedAt >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(l => l.CreatedAt < to.Value);
            }

            return query;
        }

        private IQueryable<ErrorLog> BuildErrorLogQuery(
            IReadOnlyList<string>? levels, DateTimeOffset? from, DateTimeOffset? to, string? search)
        {
            IQueryable<ErrorLog> query = dbContext.ErrorLogs;

            List<ErrorLevel> parsedLevels = (levels ?? Array.Empty<string>())
                .Select(l => Enum.TryParse(l, ignoreCase: true, out ErrorLevel parsed) ? (ErrorLevel?)parsed : null)
                .Where(l => l is not null)
                .Select(l => l!.Value)
                .ToList();

            query = parsedLevels.Count > 0
                ? query.Where(e => parsedLevels.Contains(e.Level))
                // Default view: error+fatal only, per spec.
                : query.Where(e => e.Level == ErrorLevel.Error || e.Level == ErrorLevel.Fatal);

            if (from is not null)
            {
                query = query.Where(e => e.CreatedAt >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(e => e.CreatedAt < to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern = $"%{search.Trim()}%";
                query = query.Where(e => EF.Functions.ILike(e.Message, pattern));
            }

            return query;
        }

        private static SmsLogItem ToSmsLogItem(SmsLog log) => new(
            log.Id, IsraeliMobilePhoneValidator.MaskForLogging(log.ToPhoneNumber), log.Type.ToString(),
            log.Success, log.ProviderStatusCode, log.CreatedAt);

        private static KpiValue MakeKpi(int current, int previous)
        {
            double? changePercent = previous == 0
                ? (current == 0 ? 0 : null)
                : Math.Round((current - previous) / (double)previous * 100, 1);

            return new KpiValue(current, previous, changePercent);
        }

        private async Task WriteAuditAsync(string action, Guid entityId, CancellationToken cancellationToken)
        {
            await dbContext.AuditLog.AddAsync(new AuditLogEntry
            {
                Id = Guid.CreateVersion7(),
                Actor = "admin",
                Action = action,
                EntityType = "subscriber",
                EntityId = entityId,
                At = DateTimeOffset.UtcNow,
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
