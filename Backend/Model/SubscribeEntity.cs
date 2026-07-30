namespace Tanakh.Model
{
    public class SubscribeEntity
    {
        public required string UserName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string SelectedTime { get; set; }
    }

    public class UnSubscribe
    {
        public required string PhoneNumber { get; set; }
    }

    public class EmailMessage
    {
        public required string Subject { get; set; }
        public required string Body { get; set; }
    }
}
