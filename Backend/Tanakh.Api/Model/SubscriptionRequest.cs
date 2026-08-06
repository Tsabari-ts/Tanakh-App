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

        // Checked via ISubscriptionService.VerifyOtpAsync before any
        // subscriber/consent work - see SubscriptionsController.
        public required string OtpCode { get; set; }

        // lastUpdated of LEGAL_DOCS['terms']/['privacy'] on the frontend at
        // the moment consent was shown, plus the exact consent sentence
        // rendered - persisted verbatim to ConsentRecord for Amendment 13
        // proof-of-consent purposes.
        public required string TermsVersion { get; set; }

        public required string PrivacyVersion { get; set; }

        public required string ConsentText { get; set; }
    }
}
