using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Api.Controllers
{
    // Anonymous - the public app (maintenance interstitial, announcement
    // banner) and any future flag-gated feature all need to read these
    // without a login.
    [Route("api/v1/system")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IAppSettingsService appSettingsService;
        private readonly AppDbContext dbContext;

        public SystemController(IAppSettingsService appSettingsService, AppDbContext dbContext)
        {
            this.appSettingsService = appSettingsService;
            this.dbContext = dbContext;
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> GetMaintenanceAsync(CancellationToken cancellationToken)
        {
            MaintenanceStatus status = await appSettingsService.GetMaintenanceAsync(cancellationToken);
            return Ok(status);
        }

        [HttpGet("banner")]
        public async Task<IActionResult> GetBannerAsync(CancellationToken cancellationToken)
        {
            BannerStatus? banner = await appSettingsService.GetBannerAsync(cancellationToken);

            // Expiry is filtered here, not in the service - the admin editor
            // reads the same underlying row and needs to see it even once
            // expired (to review/extend it), so the cutoff only applies to
            // this public-facing read.
            if (banner is null || banner.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Ok((BannerStatus?)null);
            }

            return Ok(banner);
        }

        [HttpGet("flags")]
        public async Task<IActionResult> GetFlagsAsync(CancellationToken cancellationToken)
        {
            Dictionary<string, bool> flags = await dbContext.FeatureFlags
                .ToDictionaryAsync(f => f.Name, f => f.Enabled, cancellationToken);
            return Ok(flags);
        }
    }
}
