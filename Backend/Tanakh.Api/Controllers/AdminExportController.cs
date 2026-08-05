using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Csv;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/admin/export")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminExportController : ControllerBase
    {
        private readonly IAdminService adminService;

        public AdminExportController(IAdminService adminService)
        {
            this.adminService = adminService;
        }

        // resource: "users" | "sms-log" | "error-log". Filters mirror the
        // corresponding list endpoint's query params, so "export what I'm
        // currently looking at" is a straightforward client-side mapping.
        [HttpGet("{resource}")]
        public async Task<IActionResult> ExportAsync(
            string resource,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? level,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            CancellationToken cancellationToken)
        {
            string? csv = resource.ToLowerInvariant() switch
            {
                "users" => await BuildUsersCsvAsync(search, status, from, to, cancellationToken),
                "sms-log" => await BuildSmsLogCsvAsync(status, from, to, cancellationToken),
                "error-log" => await BuildErrorLogCsvAsync(level, from, to, search, cancellationToken),
                _ => null
            };

            if (csv is null)
            {
                return NotFound();
            }

            // UTF-8 BOM so Excel renders the Hebrew columns correctly
            // instead of guessing the wrong codepage.
            byte[] bytes = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv)];
            return File(bytes, "text/csv; charset=utf-8", $"{resource}.csv");
        }

        private async Task<string> BuildUsersCsvAsync(
            string? search, string? status, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            IReadOnlyList<UserListItem> items = await adminService.ExportUsersAsync(search, status, from, to, cancellationToken);

            return CsvWriter.Write(
                ["id", "phone_number", "display_name", "status", "created_at", "updated_at"],
                items.Select(item => (IReadOnlyList<string?>)
                [
                    item.Id.ToString(), item.PhoneNumber, item.DisplayName, item.Status,
                    item.CreatedAt.ToString("O"), item.UpdatedAt.ToString("O")
                ]));
        }

        private async Task<string> BuildSmsLogCsvAsync(
            string? status, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
        {
            bool? success = status?.ToLowerInvariant() switch
            {
                "sent" => true,
                "failed" => false,
                _ => null
            };

            IReadOnlyList<SmsLogItem> items = await adminService.ExportSmsLogAsync(success, from, to, cancellationToken);

            return CsvWriter.Write(
                ["id", "phone", "type", "success", "provider_status_code", "created_at"],
                items.Select(item => (IReadOnlyList<string?>)
                [
                    item.Id.ToString(), item.MaskedPhone, item.Type, item.Success.ToString(),
                    item.ProviderStatusCode?.ToString(), item.CreatedAt.ToString("O")
                ]));
        }

        private async Task<string> BuildErrorLogCsvAsync(
            string? level, DateTimeOffset? from, DateTimeOffset? to, string? search, CancellationToken cancellationToken)
        {
            IReadOnlyList<string>? levels = string.IsNullOrWhiteSpace(level)
                ? null
                : level.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            IReadOnlyList<ErrorLogItem> items = await adminService.ExportErrorLogAsync(levels, from, to, search, cancellationToken);

            return CsvWriter.Write(
                ["id", "level", "message", "endpoint", "status_code", "resolved", "created_at"],
                items.Select(item => (IReadOnlyList<string?>)
                [
                    item.Id.ToString(), item.Level, item.Message, item.Endpoint,
                    item.StatusCode?.ToString(), item.Resolved.ToString(), item.CreatedAt.ToString("O")
                ]));
        }
    }
}
