using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private const string MaintenanceKey = "maintenance";
        private const string BannerKey = "banner";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AppDbContext dbContext;
        private readonly IMemoryCache cache;

        public AppSettingsService(AppDbContext dbContext, IMemoryCache cache)
        {
            this.dbContext = dbContext;
            this.cache = cache;
        }

        public async Task<MaintenanceStatus> GetMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheKeyFor(MaintenanceKey);
            if (cache.TryGetValue(cacheKey, out MaintenanceStatus? cached) && cached is not null)
            {
                return cached;
            }

            AppSetting? row = await dbContext.AppSettings.FindAsync([MaintenanceKey], cancellationToken);
            MaintenanceStatus status = row is null
                ? new MaintenanceStatus(false, null)
                : JsonSerializer.Deserialize<MaintenanceStatus>(row.ValueJson) ?? new MaintenanceStatus(false, null);

            SetCache(cacheKey, status);
            return status;
        }

        public async Task SetMaintenanceAsync(bool enabled, string? message, CancellationToken cancellationToken = default)
        {
            await UpsertAsync(MaintenanceKey, new MaintenanceStatus(enabled, message), cancellationToken);
            cache.Remove(CacheKeyFor(MaintenanceKey));
        }

        public async Task<BannerStatus?> GetBannerAsync(CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheKeyFor(BannerKey);
            if (cache.TryGetValue(cacheKey, out BannerStatus? cached))
            {
                return cached;
            }

            AppSetting? row = await dbContext.AppSettings.FindAsync([BannerKey], cancellationToken);
            BannerStatus? status = row is null ? null : JsonSerializer.Deserialize<BannerStatus>(row.ValueJson);

            SetCache(cacheKey, status);
            return status;
        }

        public async Task SetBannerAsync(string text, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            await UpsertAsync(BannerKey, new BannerStatus(text, expiresAt), cancellationToken);
            cache.Remove(CacheKeyFor(BannerKey));
        }

        public async Task ClearBannerAsync(CancellationToken cancellationToken = default)
        {
            AppSetting? row = await dbContext.AppSettings.FindAsync([BannerKey], cancellationToken);
            if (row is not null)
            {
                dbContext.AppSettings.Remove(row);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            cache.Remove(CacheKeyFor(BannerKey));
        }

        private async Task UpsertAsync<T>(string key, T value, CancellationToken cancellationToken)
        {
            AppSetting? row = await dbContext.AppSettings.FindAsync([key], cancellationToken);
            string json = JsonSerializer.Serialize(value);

            if (row is null)
            {
                await dbContext.AppSettings.AddAsync(new AppSetting { Key = key, ValueJson = json }, cancellationToken);
            }
            else
            {
                row.ValueJson = json;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Size must be set explicitly - the shared IMemoryCache has a
        // SizeLimit configured (see Program.cs), so an unsized entry throws.
        private void SetCache<T>(string key, T value) =>
            cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration, Size = 1 });

        private static string CacheKeyFor(string settingKey) => $"app-setting:{settingKey}";
    }
}
