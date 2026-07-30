using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;

namespace Tanakh.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SubscribeController : ControllerBase
    {
        private readonly IEmailSender emailSender;

        public SubscribeController(IEmailSender emailSender)
        {
            this.emailSender = emailSender;
        }

        // No CancellationToken parameter on these actions, deliberately: the only
        // I/O here is the notification email, and IEmailSender.SendMessageAsync is
        // designed to always run to completion regardless of client disconnect -
        // the site owner still wants to know about the subscribe/unsubscribe
        // request even if the visitor's tab closed right after submitting.

        /// <summary>Notifies the site owner by email that a new user wants to subscribe to reminders.</summary>
        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterNewUserAsync([FromBody] SubscribeEntity subscribeEntity)
        {
            EmailMessage emailMessage = new EmailMessage
            {
                Subject = "הוספת משתמש חדש",
                Body = $"משתמש חדש מעוניין להצטרף לרשימה, הנה הפרטים שלו:\nשם המשתמש: {subscribeEntity.UserName}\nמספר הפלאפון: {subscribeEntity.PhoneNumber}\nשעת התזכורת: {subscribeEntity.SelectedTime}"
            };

            bool isSuccessful = await emailSender.SendMessageAsync(emailMessage);

            // Success stays a bare `true`, deliberately not wrapped in an object:
            // the Angular frontend does `this.subscribeSuccessful = response` and
            // relies on the body itself being the boolean. Failure is mapped to a
            // real error status instead of a "soft" 200 with `false` in the body -
            // an object body there would be truthy in JS even when it represents
            // failure, which would have silently broken the frontend's own check.
            if (!isSuccessful)
            {
                return Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Failed to send notification email.",
                    detail: "The email notification for this subscription request could not be delivered.");
            }

            return Ok(true);
        }

        /// <summary>Notifies the site owner by email that an existing user wants to unsubscribe.</summary>
        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUserAsync([FromBody] UnSubscribe unSubscribe)
        {
            EmailMessage emailMessage = new EmailMessage
            {
                Subject = "הסרת משתמש קיים",
                Body = $"משתמש קיים מעוניין לצאת מהרשימה, הנה מספר הפלאפון שלו: {unSubscribe.PhoneNumber}"
            };

            bool isSuccessful = await emailSender.SendMessageAsync(emailMessage);

            if (!isSuccessful)
            {
                return Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Failed to send notification email.",
                    detail: "The email notification for this unsubscribe request could not be delivered.");
            }

            return Ok(true);
        }
    }
}
