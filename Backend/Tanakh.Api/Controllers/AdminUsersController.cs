using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/users")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminUsersController : ControllerBase
    {
        private const int DefaultLimit = 25;

        private readonly IAdminService adminService;

        public AdminUsersController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersAsync(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int limit = DefaultLimit,
            CancellationToken cancellationToken = default)
        {
            PagedResult<UserListItem> result = await adminService.GetUsersAsync(
                search, status, from, to, Math.Max(page, 1), Math.Clamp(limit, 1, 200), cancellationToken);
            return Ok(result);
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateStatusAsync(
            Guid id, [FromBody] AdminUserActionRequest request, CancellationToken cancellationToken)
        {
            Task<bool> action = request.Action.ToLowerInvariant() switch
            {
                "block" => adminService.BlockUserAsync(id, cancellationToken),
                "unblock" => adminService.UnblockUserAsync(id, cancellationToken),
                _ => null!
            };

            if (action is null)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_action", detail: "Expected \"block\" or \"unblock\".");
            }

            bool found = await action;
            return found ? Ok() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            bool found = await adminService.DeleteUserAsync(id, cancellationToken);
            return found ? Ok() : NotFound();
        }
    }
}
