using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Auth;
using Tanakh.Api.Model;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Api.Controllers
{
    // The auth boundary itself - endpoints here are anonymous by necessity,
    // protected instead by rate limiting (login) and OTP attempt limits
    // (verify-otp). Every attempt (success/failure) is written to AuditLog.
    [Route("api/v1/admin/auth")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private const int OtpValidityMinutes = 5;
        private const int MaxOtpAttempts = 3;

        private readonly AppDbContext dbContext;
        private readonly IHashingService hashingService;
        private readonly IAdminPasswordHasher passwordHasher;
        private readonly ISmsSender smsSender;
        private readonly AdminOptions adminOptions;

        public AdminAuthController(
            AppDbContext dbContext,
            IHashingService hashingService,
            IAdminPasswordHasher passwordHasher,
            ISmsSender smsSender,
            IOptions<AdminOptions> adminOptions)
        {
            this.dbContext = dbContext;
            this.hashingService = hashingService;
            this.passwordHasher = passwordHasher;
            this.smsSender = smsSender;
            this.adminOptions = adminOptions.Value;
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimiterPolicyNames.AdminLogin)]
        public async Task<IActionResult> LoginAsync([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
        {
            // Fail closed if Admin:Username/PasswordHash were never
            // configured - same reasoning as the old BasicAuthenticationHandler.
            bool configured = !string.IsNullOrEmpty(adminOptions.Username) && !string.IsNullOrEmpty(adminOptions.PasswordHash);

            bool credentialsValid = configured
                && FixedTimeEquals(request.Username, adminOptions.Username)
                && passwordHasher.Verify(request.Password, adminOptions.PasswordHash);

            if (!credentialsValid)
            {
                await LogAuditAsync("admin.login.failure", cancellationToken);
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "invalid_credentials");
            }

            string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            // Invalidate any prior unused OTP - only one can be live at a time.
            await dbContext.OtpCodes
                .Where(o => !o.Used)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Used, true), cancellationToken);

            await dbContext.OtpCodes.AddAsync(new OtpCode
            {
                Id = Guid.CreateVersion7(),
                CodeHash = hashingService.Hash(code),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpValidityMinutes),
                Attempts = 0,
                Used = false,
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await smsSender.SendAsync(
                adminOptions.Phone,
                $"קוד האימות שלך למערכת הניהול: {code} (בתוקף ל-{OtpValidityMinutes} דקות)",
                cancellationToken);

            return Ok(new { otpRequired = true });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtpAsync([FromBody] AdminVerifyOtpRequest request, CancellationToken cancellationToken)
        {
            OtpCode? otp = await dbContext.OtpCodes
                .Where(o => !o.Used && o.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp is null)
            {
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "otp_invalid");
            }

            string suppliedHash = hashingService.Hash(request.Code);
            bool match = FixedTimeEquals(suppliedHash, otp.CodeHash);

            if (!match)
            {
                otp.Attempts++;
                bool locked = otp.Attempts >= MaxOtpAttempts;
                if (locked)
                {
                    otp.Used = true;
                }
                await dbContext.SaveChangesAsync(cancellationToken);

                await LogAuditAsync(locked ? "admin.otp.locked" : "admin.otp.failure", cancellationToken);
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: locked ? "otp_locked" : "otp_invalid");
            }

            otp.Used = true;
            await dbContext.SaveChangesAsync(cancellationToken);

            ClaimsIdentity identity = new(
                new[]
                {
                    new Claim(ClaimTypes.Name, adminOptions.Username),
                    new Claim(ClaimTypes.Role, "admin"),
                },
                AdminCookieAuthDefaults.SchemeName);

            await HttpContext.SignInAsync(AdminCookieAuthDefaults.SchemeName, new ClaimsPrincipal(identity));
            await LogAuditAsync("admin.login.success", cancellationToken);
            return Ok();
        }

        // No-op authenticated probe - lets the Angular route guard check
        // "is there a valid session" (401 if not) without a side effect,
        // unlike logout.
        [HttpGet("session")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Session() => Ok();

        [HttpPost("logout")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
        {
            await HttpContext.SignOutAsync(AdminCookieAuthDefaults.SchemeName);
            await LogAuditAsync("admin.logout", cancellationToken);
            return Ok();
        }

        private async Task LogAuditAsync(string action, CancellationToken cancellationToken)
        {
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers.UserAgent.ToString();

            await dbContext.AuditLog.AddAsync(new AuditLogEntry
            {
                Id = Guid.CreateVersion7(),
                Actor = "admin",
                Action = action,
                EntityType = "admin_session",
                IpHash = ip is null ? null : hashingService.Hash(ip),
                Metadata = JsonSerializer.Serialize(new { userAgent }),
                At = DateTimeOffset.UtcNow,
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
