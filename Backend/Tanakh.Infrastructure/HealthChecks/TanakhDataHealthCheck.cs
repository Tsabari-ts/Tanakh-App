using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Infrastructure.HealthChecks
{
    public class TanakhDataHealthCheck : IHealthCheck
    {
        private readonly CacheProvider cacheProvider;

        public TanakhDataHealthCheck(CacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // No file paths in the result - health check output must not leak
            // filesystem details.
            HealthCheckResult result = cacheProvider.DataFilesExist()
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Tanakh data files are missing.");

            return Task.FromResult(result);
        }
    }
}
