using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/logs")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminLogsController : ControllerBase
    {
        private const int DefaultLimit = 25;

        private readonly IAdminService adminService;

        public AdminLogsController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        // level: comma-separated (e.g. "error,fatal"). Omitted -> AdminService
        // defaults to error+fatal only, per spec.
        [HttpGet]
        public async Task<IActionResult> GetLogsAsync(
            [FromQuery] string? level,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int limit = DefaultLimit,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<string>? levels = string.IsNullOrWhiteSpace(level)
                ? null
                : level.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            PagedResult<ErrorLogItem> result = await adminService.GetErrorLogAsync(
                levels, from, to, search, Math.Max(page, 1), Math.Clamp(limit, 1, 200), cancellationToken);
            return Ok(result);
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopErrorsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<TopError> topErrors = await adminService.GetTopErrorsAsync(cancellationToken);
            return Ok(topErrors);
        }

        [HttpPatch("{id:guid}/resolve")]
        public async Task<IActionResult> ResolveAsync(Guid id, CancellationToken cancellationToken)
        {
            bool found = await adminService.ResolveErrorAsync(id, cancellationToken);
            return found ? Ok() : NotFound();
        }

        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupAsync([FromBody] AdminCleanupLogsRequest request, CancellationToken cancellationToken)
        {
            if (request.OlderThanDays <= 0)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_older_than_days");
            }

            int deleted = await adminService.CleanupErrorLogAsync(request.OlderThanDays, cancellationToken);
            return Ok(new { deleted });
        }
    }
}
