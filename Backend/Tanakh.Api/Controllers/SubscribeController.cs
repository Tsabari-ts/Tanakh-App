using Microsoft.AspNetCore.Mvc;
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

        /// <summary>Notifies the site owner by email that a new user wants to subscribe to reminders.</summary>
        [HttpPost("RegisterUser")]
        public IActionResult RegisterNewUser([FromBody] SubscribeEntity subscribeEntity)
        {
            bool isSuccessful = false;

            EmailMessage emailMessage = new EmailMessage
            {
                Subject = "הוספת משתמש חדש",
                Body = $"משתמש חדש מעוניין להצטרף לרשימה, הנה הפרטים שלו:\nשם המשתמש: {subscribeEntity.UserName}\nמספר הפלאפון: {subscribeEntity.PhoneNumber}\nשעת התזכורת: {subscribeEntity.SelectedTime}"
            };

            isSuccessful = emailSender.SendMessage(emailMessage);
            return Ok(isSuccessful);
        }

        /// <summary>Notifies the site owner by email that an existing user wants to unsubscribe.</summary>
        [HttpPost("DeleteUser")]
        public IActionResult DeleteUser([FromBody] UnSubscribe unSubscribe)
        {
            bool isSuccessful = false;

            EmailMessage emailMessage = new EmailMessage
            {
                Subject = "הסרת משתמש קיים",
                Body = $"משתמש קיים מעוניין לצאת מהרשימה, הנה מספר הפלאפון שלו: {unSubscribe.PhoneNumber}"
            };

            isSuccessful = emailSender.SendMessage(emailMessage);
            return Ok(isSuccessful);
        }
    }
}