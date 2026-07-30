using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Api.Pages;
using Tanakh.Domain;
using Tanakh.Domain.Validation;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/subscriptions")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService subscriptionService;
        private readonly IUnsubscribeTokenService unsubscribeTokenService;
        private readonly RemindersOptions options;

        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            IUnsubscribeTokenService unsubscribeTokenService,
            IOptions<RemindersOptions> options)
        {
            this.subscriptionService = subscriptionService;
            this.unsubscribeTokenService = unsubscribeTokenService;
            this.options = options.Value;
        }

        /// <summary>Signs up a new subscriber (or restarts opt-in for a lapsed one) for daily reading reminders.</summary>
        [HttpPost]
        public async Task<IActionResult> SubscribeAsync([FromBody] SubscriptionRequest request, CancellationToken cancellationToken)
        {
            if (!request.Consent)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Consent is required to subscribe.");
            }

            if (!IsValidEmail(request.Email))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid email address.");
            }

            if (!TimeOnly.TryParse(request.PreferredTime, out TimeOnly preferredTime))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid preferredTime - expected HH:mm.");
            }

            if (!TimeZoneValidator.IsValidIanaId(request.Timezone))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid timezone - expected an IANA zone id.");
            }

            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers.UserAgent.ToString();

            // Always 202, regardless of whether the address was already
            // registered - a different response here would let a caller
            // enumerate which emails are already subscribed.
            await subscriptionService.SubscribeAsync(
                request.Email,
                request.DisplayName,
                preferredTime,
                request.Timezone,
                request.SkipShabbatHolidays,
                ipAddress,
                userAgent,
                cancellationToken);

            return Accepted();
        }

        /// <summary>Completes double opt-in for a pending subscriber via the link sent in the confirmation email.</summary>
        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmAsync([FromQuery] string token, CancellationToken cancellationToken)
        {
            ConfirmationOutcome outcome = await subscriptionService.ConfirmAsync(token, cancellationToken);

            string html = outcome switch
            {
                ConfirmationOutcome.Confirmed => SubscriptionPages.Render(
                    "ההרשמה אושרה!",
                    "התזכורת היומית שלך תתחיל להישלח בקרוב.",
                    options.PublicBaseUrl),
                ConfirmationOutcome.AlreadyUsed => SubscriptionPages.Render(
                    "הקישור כבר נוצל",
                    "ההרשמה שלך כבר אושרה בעבר.",
                    options.PublicBaseUrl),
                ConfirmationOutcome.Expired => SubscriptionPages.Render(
                    "הקישור פג תוקף",
                    "יש להירשם מחדש כדי לקבל קישור אישור חדש.",
                    options.PublicBaseUrl),
                _ => SubscriptionPages.Render(
                    "קישור לא תקין",
                    "קישור האישור אינו תקין.",
                    options.PublicBaseUrl),
            };

            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>Unsubscribes via the link a human clicks in a reminder email footer - single click, no login, no confirmation step.</summary>
        [HttpGet("unsubscribe")]
        public async Task<IActionResult> UnsubscribeViaLinkAsync([FromQuery] string token, CancellationToken cancellationToken)
        {
            bool valid = unsubscribeTokenService.TryValidate(token, out Guid subscriberId);

            if (valid)
            {
                await subscriptionService.UnsubscribeAsync(subscriberId, cancellationToken);
            }

            string html = valid
                ? SubscriptionPages.Render(
                    "הביטול בוצע",
                    "לא יישלחו אליך עוד תזכורות. נרשמתי בטעות? אפשר להירשם מחדש דרך האתר.",
                    options.PublicBaseUrl)
                : SubscriptionPages.Render(
                    "קישור לא תקין",
                    "קישור הביטול אינו תקין.",
                    options.PublicBaseUrl);

            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>RFC 8058 One-Click unsubscribe - invoked by the mail provider itself (List-Unsubscribe-Post), not by a human viewing a page. Must act immediately and return a bare 200.</summary>
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> UnsubscribeOneClickAsync([FromQuery] string token, CancellationToken cancellationToken)
        {
            if (unsubscribeTokenService.TryValidate(token, out Guid subscriberId))
            {
                await subscriptionService.UnsubscribeAsync(subscriberId, cancellationToken);
            }

            return Ok();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
