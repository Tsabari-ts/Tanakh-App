using System;
using System.Net;
using System.Net.Mail;
using Tanakh.Model;

namespace Tanakh
{
    public class EmailSender
    {
        private readonly CredentialsManager credentialsManager;

        public EmailSender(CredentialsManager credentialsManager)
        {
            this.credentialsManager = credentialsManager;
        }

        public bool SendMessage(EmailMessage emailMessage)
        {
            Credentials credentials = new Credentials();

            credentials = credentialsManager.LoadCredentials();

            bool isSuccessful = false;

            using (SmtpClient smtpClient = new SmtpClient(credentials.SmtpServer, Convert.ToInt32(credentials.SmtpPort)))
            {

                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(credentials.EmailAddress, credentials.Password);
                MailMessage message = new MailMessage(credentials.EmailAddress, credentials.RecipientAddress)
                {
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body
                };

                try
                {
                    smtpClient.Send(message);
                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }

            return isSuccessful;
        }
    }
}