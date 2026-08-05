using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/sms")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSmsController : ControllerBase
    {
        private const int DefaultLimit = 25;

        private readonly IAdminService adminService;
        private readonly ISmsBalanceService smsBalanceService;
        private readonly AdminOptions adminOptions;

        public AdminSmsController(IAdminService adminService, ISmsBalanceService smsBalanceService, IOptions<AdminOptions> adminOptions)
        {
            this.adminService = adminService;
            this.smsBalanceService = smsBalanceService;
            this.adminOptions = adminOptions.Value;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalanceAsync(CancellationToken cancellationToken)
        {
            SmsBalanceResult result = await smsBalanceService.GetBalanceAsync(cancellationToken);
            return Ok(new
            {
                result.Ok,
                result.Balance,
                result.Error,
                lowBalanceThreshold = adminOptions.LowBalanceThreshold
            });
        }

        // status: "sent" | "failed" - matches the vocabulary used in the
        // dashboard UI rather than the raw Success bool.
        [HttpGet("log")]
        public async Task<IActionResult> GetLogAsync(
            [FromQuery] string? status,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int limit = DefaultLimit,
            CancellationToken cancellationToken = default)
        {
            bool? success = status?.ToLowerInvariant() switch
            {
                "sent" => true,
                "failed" => false,
                _ => null
            };

            PagedResult<SmsLogItem> result = await adminService.GetSmsLogAsync(
                success, from, to, Math.Max(page, 1), Math.Clamp(limit, 1, 200), cancellationToken);
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStatsAsync(CancellationToken cancellationToken)
        {
            SmsStats stats = await adminService.GetSmsStatsAsync(cancellationToken);
            return Ok(stats);
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestAsync(CancellationToken cancellationToken)
        {
            await adminService.SendTestSmsAsync(cancellationToken);
            return Ok();
        }
    }
}
