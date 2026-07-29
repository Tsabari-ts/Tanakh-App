namespace Tanakh.Model
{
    public class SubscribeEntity
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string SelectedTime { get; set; }
    }

    public class UnSubscribe
    {
        public string PhoneNumber { get; set; }
    }

    public class EmailMessage
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}