using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Auth;
using Tanakh.Api.Pages;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService adminService;

        public AdminController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
        {
            AdminDashboard dashboard = await adminService.GetDashboardAsync(cancellationToken);
            string html = AdminPages.RenderDashboard(dashboard, "/api/v1/admin/actions");
            return Content(html, "text/html; charset=utf-8");
        }

        [HttpPost("actions/unsubscribe")]
        public async Task<IActionResult> UnsubscribeAsync([FromForm] string phoneNumber, CancellationToken cancellationToken)
        {
            await adminService.UnsubscribeByPhoneAsync(phoneNumber, cancellationToken);
            return Redirect("/api/v1/admin");
        }

        [HttpPost("actions/requeue")]
        public async Task<IActionResult> RequeueAsync([FromForm] Guid deliveryId, CancellationToken cancellationToken)
        {
            await adminService.RequeueFailedDeliveryAsync(deliveryId, cancellationToken);
            return Redirect("/api/v1/admin");
        }
    }
}
