using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/stats")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminStatsController : ControllerBase
    {
        private readonly IAdminService adminService;

        public AdminStatsController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewAsync(
            [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
        {
            DateTimeOffset rangeTo = to ?? DateTimeOffset.UtcNow;
            DateTimeOffset rangeFrom = from ?? rangeTo.AddDays(-7);

            AdminOverview overview = await adminService.GetOverviewAsync(rangeFrom, rangeTo, cancellationToken);
            return Ok(overview);
        }
    }
}
