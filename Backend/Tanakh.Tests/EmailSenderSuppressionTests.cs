using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Tests
{
    public class EmailSenderSuppressionTests
    {
        private sealed class AlwaysSuppressedService : ISuppressionService
        {
            public Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default) =>
                Task.FromResult(true);
        }

        [Fact]
        public async Task SendMessageAsync_Never_Dispatches_To_A_Suppressed_Recipient()
        {
            // SmtpServer is deliberately unroutable - if EmailSender ever
            // tried to actually send, this would hang/fail on a real
            // connection attempt instead of returning quickly with false.
            EmailOptions emailOptions = new EmailOptions
            {
                EmailAddress = "sender@example.com",
                Password = "unused",
                RecipientAddress = "suppressed@example.com",
                SmtpServer = "smtp.invalid.example",
                SmtpPort = 587
            };

            EmailSender sender = new EmailSender(Options.Create(emailOptions), new AlwaysSuppressedService());

            EmailMessage message = new EmailMessage
            {
                To = "suppressed@example.com",
                Subject = "test",
                Body = "test"
            };

            bool result = await sender.SendMessageAsync(message);

            Assert.False(result);
        }
    }
}
