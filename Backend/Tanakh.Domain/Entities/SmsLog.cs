using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // One row per ISmsSender.SendAsync call, regardless of caller - written
    // by Sms4FreeSmsSender itself (the one place that knows every send's
    // true outcome), not duplicated at each call site. Distinct from
    // ReminderDelivery, which tracks the reminder scheduler's own
    // per-delivery state; this is a flat, admin-facing log across every
    // message type (reminder/OTP/test).
    public class SmsLog : IHasCreatedAt
    {
        public Guid Id { get; set; }

        // E.164, stored raw - masked only at the API-response layer
        // (IsraeliMobilePhoneValidator.MaskForLogging), never in the DB.
        public required string ToPhoneNumber { get; set; }

        public SmsMessageType Type { get; set; }

        public string? Message { get; set; }

        public bool Success { get; set; }

        // Raw response body from SMS4FREE (or "dry-run" when Sms:DryRun is
        // true), verbatim - same rationale as ReminderDelivery.ProviderResponse.
        public string? ProviderResponse { get; set; }

        public int? ProviderStatusCode { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
