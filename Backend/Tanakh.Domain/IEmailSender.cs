using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public interface IEmailSender
    {
        Task<bool> SendMessageAsync(EmailMessage emailMessage);
    }
}
