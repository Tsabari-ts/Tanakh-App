using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions emailOptions;

        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            this.emailOptions = emailOptions.Value;
        }

        public bool SendMessage(EmailMessage emailMessage)
        {
            bool isSuccessful = false;

            try
            {
                using (SmtpClient smtpClient = new SmtpClient(emailOptions.SmtpServer, emailOptions.SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(emailOptions.EmailAddress, emailOptions.Password);
                    MailMessage message = new MailMessage(emailOptions.EmailAddress, emailOptions.RecipientAddress)
                    {
                        Subject = emailMessage.Subject,
                        Body = emailMessage.Body
                    };

                    smtpClient.Send(message);
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
