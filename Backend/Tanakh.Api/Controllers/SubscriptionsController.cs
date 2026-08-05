using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;
using Tanakh.Domain.Validation;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/subscriptions")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService subscriptionService;
        private readonly IUnsubscribeTokenService unsubscribeTokenService;

        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            IUnsubscribeTokenService unsubscribeTokenService)
        {
            this.subscriptionService = subscriptionService;
            this.unsubscribeTokenService = unsubscribeTokenService;
        }

        /// <summary>Signs up (or updates/reactivates) a subscriber for daily SMS reading reminders. No confirmation step - phone format is validated, not verified. Returns a manage token the client stores to reach the endpoints below without a login.</summary>
        [HttpPost]
        public async Task<IActionResult> SubscribeAsync([FromBody] SubscriptionRequest request, CancellationToken cancellationToken)
        {
            if (!request.Consent)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "consent_required", detail: "Consent is required to subscribe.");
            }

            IsraeliMobilePhoneValidator.ValidationResult phoneResult = IsraeliMobilePhoneValidator.Validate(request.PhoneNumber, out string phoneE164);
            if (phoneResult != IsraeliMobilePhoneValidator.ValidationResult.Valid)
            {
                string code = phoneResult switch
                {
                    IsraeliMobilePhoneValidator.ValidationResult.Empty => "phone_required",
                    IsraeliMobilePhoneValidator.ValidationResult.Landline => "phone_landline",
                    _ => "phone_invalid"
                };
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: code, detail: "Invalid phone number.");
            }

            if (!TimeOnly.TryParse(request.PreferredTime, out TimeOnly preferredTime))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_time", detail: "Invalid preferredTime - expected HH:mm.");
            }

            if (!TimeZoneValidator.IsValidIanaId(request.Timezone))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_timezone", detail: "Invalid timezone - expected an IANA zone id.");
            }

            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers.UserAgent.ToString();

            string manageToken = await subscriptionService.SubscribeAsync(
                phoneE164,
                request.DisplayName,
                preferredTime,
                request.Timezone,
                request.SkipShabbatHolidays,
                ipAddress,
                userAgent,
                cancellationToken);

            return Ok(new SubscriptionResponse { ManageToken = manageToken });
        }

        /// <summary>Preference-center bootstrap for the Settings screen - reached via the manage token the client stored at subscribe time.</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetPreferencesAsync([FromQuery] string token, CancellationToken cancellationToken)
        {
            if (!unsubscribeTokenService.TryValidate(token, out Guid subscriberId))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_token");
            }

            SubscriberPreferences? preferences = await subscriptionService.GetPreferencesAsync(subscriberId, cancellationToken);
            if (preferences is null)
            {
                return NotFound();
            }

            return Ok(preferences);
        }

        [HttpPost("me")]
        public async Task<IActionResult> UpdatePreferencesAsync([FromBody] UpdatePreferencesRequest request, CancellationToken cancellationToken)
        {
            if (!unsubscribeTokenService.TryValidate(request.Token, out Guid subscriberId))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_token");
            }

            TimeOnly? preferredTime = request.PreferredTime is not null && TimeOnly.TryParse(request.PreferredTime, out TimeOnly parsed)
                ? parsed
                : null;
            bool pause = request.Action == "pause";
            bool resume = request.Action == "resume";

            await subscriptionService.UpdatePreferencesAsync(
                subscriberId, preferredTime, request.SkipShabbatHolidays, pause, resume, cancellationToken);

            return Ok();
        }

        /// <summary>Cancels the reminder subscription - called from the Settings screen using the stored manage token.</summary>
        [HttpPost("me/unsubscribe")]
        public async Task<IActionResult> UnsubscribeAsync([FromBody] ManageTokenRequest request, CancellationToken cancellationToken)
        {
            if (!unsubscribeTokenService.TryValidate(request.Token, out Guid subscriberId))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "invalid_token");
            }

            await subscriptionService.UnsubscribeAsync(subscriberId, cancellationToken);
            return Ok();
        }
    }
}
