namespace Tanakh.Infrastructure.Options
{
    public class SmsOptions
    {
        public const string SectionName = "Sms";

        // SMS4FREE API credentials - see Backend/README.md for how these
        // are supplied (dotnet user-secrets locally, Sms__* env vars in
        // production). Never set in appsettings.json.
        public string Key { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;

        // Currently the registered mobile number (SMS4FREE locks the sender
        // to it while on the free 10-message trial). Must switch to an
        // English business/app name once the trial is used up - and that
        // switch likely needs its own sender-verification step with
        // SMS4FREE per the 2020 telecom regulation. Not code that needs to
        // change, just this config value.
        public string Sender { get; set; } = string.Empty;

        // TODO(LAUNCH): verify this against the actual endpoint shown on
        // the SMS4FREE account's own API page before going live - do not
        // assume this is still correct without checking.
        public string ApiUrl { get; set; } = "https://api.sms4free.co.il/ApiSMS/v2/SendSMS";

        public int TimeoutSeconds { get; set; } = 15;

        // No sandbox account exists (per spec Q21) - DryRun must be true in
        // every non-production environment, enforced at startup, not just
        // by convention.
        public bool DryRun { get; set; } = true;
    }
}
