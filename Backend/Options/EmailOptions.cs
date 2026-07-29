namespace Tanakh.Options
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
    }
}
