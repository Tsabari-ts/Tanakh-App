using System.Linq;
using System.Text.RegularExpressions;

namespace Tanakh.Domain.Validation
{
    // The single source of truth for "is this an Israeli mobile number" -
    // the Angular client mirrors this exact rule (same regex, same
    // normalization steps) so the two never drift. This is format
    // validation only - actual ownership of the number is verified
    // separately via SubscriptionService.RequestOtpAsync/VerifyOtpAsync.
    public static class IsraeliMobilePhoneValidator
    {
        // Local format (after normalization): 05X followed by 7 digits - 10
        // digits total. Deliberately accepts every 05X prefix, not a
        // hardcoded allowlist - the regulator assigns new mobile prefixes
        // every few years, and an aggressive allowlist would reject real
        // customers.
        private static readonly Regex LocalMobilePattern = new(@"^05[0-9]\d{7}$", RegexOptions.Compiled);

        // Landline area codes - rejected because SMS4FREE (and SMS in
        // general) cannot deliver to them. Kept separate from the mobile
        // pattern so the "invalid landline" error message can be distinct
        // from "invalid number" in general.
        private static readonly Regex LandlinePattern = new(@"^0[23489]\d{7}$", RegexOptions.Compiled);

        public enum ValidationResult
        {
            Valid,
            Empty,
            Landline,
            Invalid
        }

        // Accepts local (0501234567, 050-123-4567), +972/00972-prefixed, and
        // arbitrary punctuation (spaces/dashes/dots/parens) - normalizes all
        // of them to local form first. Returns the DB storage format
        // (E.164, "+972501234567") via e164 when Valid.
        public static ValidationResult Validate(string? input, out string e164)
        {
            e164 = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.Empty;
            }

            string digits = new(input.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("00972"))
            {
                digits = "0" + digits[5..];
            }
            else if (digits.StartsWith("972") && digits.Length == 12)
            {
                digits = "0" + digits[3..];
            }

            if (LandlinePattern.IsMatch(digits))
            {
                return ValidationResult.Landline;
            }

            if (!LocalMobilePattern.IsMatch(digits))
            {
                return ValidationResult.Invalid;
            }

            e164 = "+972" + digits[1..];
            return ValidationResult.Valid;
        }

        // "05******67" - for logs only, never for the stored value. Masks
        // the local representation (matches how a human reads the number),
        // not the raw E.164 string.
        public static string MaskForLogging(string e164)
        {
            string local = e164.StartsWith("+972") ? "0" + e164[4..] : e164;

            if (local.Length < 5)
            {
                return "***";
            }

            return local[..2] + new string('*', local.Length - 4) + local[^2..];
        }
    }
}
