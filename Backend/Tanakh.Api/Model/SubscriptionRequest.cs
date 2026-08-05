namespace Tanakh.Api.Model
{
    public class SubscriptionRequest
    {
        // Any format the user typed (spaces/dashes/+972/00972/local) -
        // normalized and validated server-side via
        // IsraeliMobilePhoneValidator, same rule the client uses.
        public required string PhoneNumber { get; set; }

        public string? DisplayName { get; set; }

        // "HH:mm", the subscriber's local wall-clock time.
        public required string PreferredTime { get; set; }

        public string Timezone { get; set; } = "Asia/Jerusalem";

        public bool SkipShabbatHolidays { get; set; } = true;

        public bool Consent { get; set; }
    }
}
