namespace Tanakh.Api.Model
{
    public class SubscriptionRequest
    {
        public required string Email { get; set; }

        public string? DisplayName { get; set; }

        // "HH:mm", the subscriber's local wall-clock time.
        public required string PreferredTime { get; set; }

        public string Timezone { get; set; } = "Asia/Jerusalem";

        public bool SkipShabbatHolidays { get; set; } = true;

        public bool Consent { get; set; }
    }
}
