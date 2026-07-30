using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Tanakh.Domain.Caching;

namespace Tanakh.Infrastructure.Caching
{
    public class MemoryTanakhCache : ITanakhCache
    {
        // Tanakh text/structure are static, immutable data read from disk. The
        // expiration here just bounds how long a stale in-memory copy could
        // outlive a data-file update on the same running process - it is not
        // about correctness, so a long window is fine.
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(12);

        private readonly IMemoryCache cache;
        private readonly ILogger<MemoryTanakhCache> logger;

        public MemoryTanakhCache(IMemoryCache cache, ILogger<MemoryTanakhCache> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value) where T : class
        {
            if (cache.TryGetValue(key, out T? cached) && cached is not null)
            {
                logger.LogDebug("Cache hit for {CacheKey}", key);
                value = cached;
                return true;
            }

            logger.LogDebug("Cache miss for {CacheKey}", key);
            value = null;
            return false;
        }

        public void Set<T>(string key, T value) where T : class
        {
            cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Expiration,
                Size = 1
            });
        }
    }
}
