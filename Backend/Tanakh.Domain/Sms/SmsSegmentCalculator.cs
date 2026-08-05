using System;

namespace Tanakh.Domain.Sms
{
    // Segment math per the spec: GSM-7-compatible text is 160 chars for a
    // single segment / 153 per segment once split; anything outside the
    // GSM-7 basic set (Hebrew, emoji, ...) forces UCS-2 - 70 single / 67
    // multi-part. A single non-GSM-7 character (one emoji, one Hebrew
    // letter) drops the *entire* message to UCS-2, not just that character -
    // there is no mixed encoding.
    public static class SmsSegmentCalculator
    {
        private const string Gsm7BasicSet =
            "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";

        public record Result(int CharacterCount, int SegmentCount, bool IsGsm7);

        public static Result Calculate(string message)
        {
            int characterCount = message.Length;
            bool isGsm7 = IsGsm7Compatible(message);

            if (characterCount == 0)
            {
                return new Result(0, 0, isGsm7);
            }

            int singleSegmentLimit = isGsm7 ? 160 : 70;
            int multiSegmentLimit = isGsm7 ? 153 : 67;

            int segmentCount = characterCount <= singleSegmentLimit
                ? 1
                : (int)Math.Ceiling(characterCount / (double)multiSegmentLimit);

            return new Result(characterCount, segmentCount, isGsm7);
        }

        private static bool IsGsm7Compatible(string message)
        {
            foreach (char c in message)
            {
                if (Gsm7BasicSet.IndexOf(c) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
