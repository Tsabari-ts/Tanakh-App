using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/system")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSystemController : ControllerBase
    {
        private readonly IAppSettingsService appSettingsService;
        private readonly AppDbContext dbContext;

        public AdminSystemController(IAppSettingsService appSettingsService, AppDbContext dbContext)
        {
            this.appSettingsService = appSettingsService;
            this.dbContext = dbContext;
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealthAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset processStartedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            bool databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);

            long? diskFreeBytes;
            try
            {
                diskFreeBytes = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "/").AvailableFreeSpace;
            }
            catch (Exception)
            {
                // Best-effort/informational only (container filesystems don't
                // always expose this) - never let it break the health panel.
                diskFreeBytes = null;
            }

            return Ok(new
            {
                processStartedAt,
                uptimeSeconds = (long)(DateTimeOffset.UtcNow - processStartedAt).TotalSeconds,
                databaseConnected,
                diskFreeBytes,
                buildVersion = Environment.GetEnvironmentVariable("BUILD_VERSION") ?? "dev",
                // No live "last backup" timestamp - that data lives in GitHub
                // Actions (.github/workflows/backend-backup.yml), not this DB.
                backupsNote = "גיבוי אוטומטי רץ מדי יום ב-CI (GitHub Actions)."
            });
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> GetMaintenanceAsync(CancellationToken cancellationToken)
        {
            MaintenanceStatus status = await appSettingsService.GetMaintenanceAsync(cancellationToken);
            return Ok(status);
        }

        [HttpPut("maintenance")]
        public async Task<IActionResult> SetMaintenanceAsync([FromBody] AdminSetMaintenanceRequest request, CancellationToken cancellationToken)
        {
            await appSettingsService.SetMaintenanceAsync(request.Enabled, request.Message, cancellationToken);
            await LogAuditAsync("admin.system.maintenance", System.Text.Json.JsonSerializer.Serialize(new { request.Enabled }), cancellationToken);
            return Ok();
        }

        // Unlike the public GET /api/v1/system/banner, this returns the raw
        // stored value regardless of expiry - the editor needs to see/extend
        // an already-expired banner, not just whatever's currently live.
        [HttpGet("banner")]
        public async Task<IActionResult> GetBannerAsync(CancellationToken cancellationToken)
        {
            BannerStatus? banner = await appSettingsService.GetBannerAsync(cancellationToken);
            return Ok(banner);
        }

        [HttpPut("banner")]
        public async Task<IActionResult> SetBannerAsync([FromBody] AdminSetBannerRequest request, CancellationToken cancellationToken)
        {
            await appSettingsService.SetBannerAsync(request.Text, request.ExpiresAt, cancellationToken);
            await LogAuditAsync("admin.system.banner.set", metadataJson: null, cancellationToken);
            return Ok();
        }

        [HttpDelete("banner")]
        public async Task<IActionResult> ClearBannerAsync(CancellationToken cancellationToken)
        {
            await appSettingsService.ClearBannerAsync(cancellationToken);
            await LogAuditAsync("admin.system.banner.clear", metadataJson: null, cancellationToken);
            return Ok();
        }

        [HttpGet("flags")]
        public async Task<IActionResult> GetFlagsAsync(CancellationToken cancellationToken)
        {
            List<FeatureFlag> flags = await dbContext.FeatureFlags
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);
            return Ok(flags);
        }

        [HttpPut("flags/{name}")]
        public async Task<IActionResult> SetFlagAsync(string name, [FromBody] AdminSetFeatureFlagRequest request, CancellationToken cancellationToken)
        {
            FeatureFlag? flag = await dbContext.FeatureFlags.FindAsync([name], cancellationToken);

            if (flag is null)
            {
                await dbContext.FeatureFlags.AddAsync(new FeatureFlag { Name = name, Enabled = request.Enabled }, cancellationToken);
            }
            else
            {
                flag.Enabled = request.Enabled;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await LogAuditAsync("admin.system.flag.set", System.Text.Json.JsonSerializer.Serialize(new { name, request.Enabled }), cancellationToken);
            return Ok();
        }

        [HttpDelete("flags/{name}")]
        public async Task<IActionResult> DeleteFlagAsync(string name, CancellationToken cancellationToken)
        {
            int rows = await dbContext.FeatureFlags
                .Where(f => f.Name == name)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows > 0)
            {
                await LogAuditAsync("admin.system.flag.delete", System.Text.Json.JsonSerializer.Serialize(new { name }), cancellationToken);
            }

            return rows > 0 ? Ok() : NotFound();
        }

        // metadataJson is pre-serialized by the caller (with a concretely-
        // typed anonymous object, not object?) - passing a statically
        // object?-typed value into JsonSerializer.Serialize here trips the
        // VSTHRD103 analyzer (it resolves to the reflection-based overload).
        private async Task LogAuditAsync(string action, string? metadataJson, CancellationToken cancellationToken)
        {
            AuditLogEntry entry = new()
            {
                Id = Guid.CreateVersion7(),
                Actor = "admin",
                Action = action,
                EntityType = "app_setting",
                Metadata = metadataJson,
                At = DateTimeOffset.UtcNow,
            };

            await dbContext.AuditLog.AddAsync(entry, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
