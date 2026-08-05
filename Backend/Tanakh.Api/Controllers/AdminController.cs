using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    // Dashboard content (KPIs, users, SMS log, etc.) lands in later phases -
    // this controller currently only carries the two manual actions the old
    // HTML admin page exposed (T-23), now as JSON under cookie auth.
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService adminService;

        public AdminController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        [HttpPost("actions/unsubscribe")]
        public async Task<IActionResult> UnsubscribeAsync([FromBody] AdminUnsubscribeRequest request, CancellationToken cancellationToken)
        {
            bool found = await adminService.UnsubscribeByPhoneAsync(request.PhoneNumber, cancellationToken);
            return found ? Ok() : NotFound();
        }

        [HttpPost("actions/requeue")]
        public async Task<IActionResult> RequeueAsync([FromBody] AdminRequeueRequest request, CancellationToken cancellationToken)
        {
            bool found = await adminService.RequeueFailedDeliveryAsync(request.DeliveryId, cancellationToken);
            return found ? Ok() : NotFound();
        }
    }
}
