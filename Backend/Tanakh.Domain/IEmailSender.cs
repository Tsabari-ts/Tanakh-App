namespace Tanakh.Domain
{
    public interface IEmailSender
    {
        bool SendMessage(EmailMessage emailMessage);
    }
}
