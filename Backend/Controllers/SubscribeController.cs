using Microsoft.AspNetCore.Mvc;
using Tanakh.Model;

namespace Tanakh.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SubscribeController : ControllerBase
    {
        private readonly EmailSender emailSender;

        public SubscribeController(EmailSender emailSender) 
        {
            this.emailSender = emailSender;
        }

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