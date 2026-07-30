using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions emailOptions;
        private readonly ISuppressionService suppressionService;

        public EmailSender(IOptions<EmailOptions> emailOptions, ISuppressionService suppressionService)
        {
            this.emailOptions = emailOptions.Value;
            this.suppressionService = suppressionService;
        }

        public async Task<bool> SendMessageAsync(EmailMessage emailMessage)
        {
            bool isSuccessful = false;

            try
            {
                // Mandatory, not opt-in - no caller of SendMessageAsync can
                // bypass this by not remembering to check first. Any
                // failure to evaluate the check (e.g. the DB is down) also
                // falls into this try/catch and results in `false`, not a
                // send - fail closed.
                if (await suppressionService.IsSuppressedAsync(emailMessage.To))
                {
                    Console.WriteLine("Recipient is on the suppression list - message not sent.");
                    return false;
                }

                using (SmtpClient smtpClient = new SmtpClient(emailOptions.SmtpServer, emailOptions.SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(emailOptions.EmailAddress, emailOptions.Password);
                    MailMessage message = new MailMessage(emailOptions.EmailAddress, emailMessage.To)
                    {
                        Subject = emailMessage.Subject,
                        Body = emailMessage.Body
                    };

                    // Deliberately CancellationToken.None, not request-bound: the site
                    // owner still wants this notification even if the visitor's
                    // client disconnects immediately after submitting.
                    await smtpClient.SendMailAsync(message, CancellationToken.None);
                    isSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }

            return isSuccessful;
        }
    }
}
