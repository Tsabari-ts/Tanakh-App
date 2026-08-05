using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class SmsBalanceService : ISmsBalanceService
    {
        private const string CacheKey = "sms-balance";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly HttpClient httpClient;
        private readonly SmsOptions options;
        private readonly IMemoryCache cache;
        private readonly ILogger<SmsBalanceService> logger;

        public SmsBalanceService(
            HttpClient httpClient, IOptions<SmsOptions> options, IMemoryCache cache, ILogger<SmsBalanceService> logger)
        {
            this.httpClient = httpClient;
            this.options = options.Value;
            this.cache = cache;
            this.logger = logger;
        }

        public async Task<SmsBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(CacheKey, out SmsBalanceResult? cached) && cached is not null)
            {
                return cached;
            }

            SmsBalanceResult result = await FetchBalanceAsync(cancellationToken);

            // Cache both outcomes - a broken SMS4FREE account shouldn't be
            // hammered every dashboard refresh either. Size must be set
            // explicitly - the shared IMemoryCache has a SizeLimit
            // configured (see Program.cs), which makes an unsized entry
            // throw instead of just being uncounted.
            cache.Set(CacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                Size = 1
            });
            return result;
        }

        private async Task<SmsBalanceResult> FetchBalanceAsync(CancellationToken cancellationToken)
        {
            AvailableSmsRequest request = new(options.Key, options.User, options.Pass);

            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(options.BalanceApiUrl, request, cancellationToken);
                string rawBody = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

                if (!int.TryParse(rawBody, out int balance))
                {
                    logger.LogWarning("SMS4FREE balance check returned an unparseable response: {RawBody}", rawBody);
                    return new SmsBalanceResult(Ok: false, Balance: null, Error: $"unexpected response: {rawBody}");
                }

                if (balance <= 0)
                {
                    logger.LogWarning("SMS4FREE balance check failed with status {Status}.", balance);
                    return new SmsBalanceResult(Ok: false, Balance: null, Error: $"sms4free error code {balance}");
                }

                return new SmsBalanceResult(Ok: true, Balance: balance, Error: null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "SMS4FREE balance check failed at the transport level (timeout/network).");
                return new SmsBalanceResult(Ok: false, Balance: null, Error: ex.Message);
            }
        }

        private record AvailableSmsRequest(
            [property: JsonPropertyName("key")] string Key,
            [property: JsonPropertyName("user")] string User,
            [property: JsonPropertyName("pass")] string Pass);
    }
}
