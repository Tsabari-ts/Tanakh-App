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
            return Ok(isSuccessful);
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
            return Ok(isSuccessful);
        }
    }
}
