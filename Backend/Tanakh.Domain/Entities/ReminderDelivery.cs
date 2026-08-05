using System;
using System.Security.Cryptography;
using System.Text;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // One row per subscriber per scheduled send. idempotency_key lets a
    // retried scheduler run compute the same key and hit the unique index
    // instead of double-sending - see ComputeIdempotencyKey.
    public class ReminderDelivery : IHasCreatedAt, IHasUpdatedAt
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }

        public DateTimeOffset ScheduledFor { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public DeliveryStatus Status { get; set; }

        public string? ProviderMessageId { get; set; }

        public int AttemptCount { get; set; }

        public DateTimeOffset? NextAttemptAt { get; set; }

        public string? LastError { get; set; }

        public required string IdempotencyKey { get; set; }

        public string? TargetUrl { get; set; }

        // The exact SMS text sent (after {שם}/link substitution) - needed to
        // answer "what did this subscriber actually receive" and to compute
        // SegmentCount after the fact.
        public string? MessageBody { get; set; }

        // Segments SMS4FREE actually billed for this send - UCS-2 (Hebrew)
        // is 70 chars/segment single, 67/segment multi-part. Recorded from
        // the message actually sent, not the template.
        public int? SegmentCount { get; set; }

        // Raw response body from SMS4FREE, verbatim - the only thing that
        // makes a real delivery failure debuggable after the fact.
        public string? ProviderResponse { get; set; }

        // SMS4FREE's "status" field: >0 recipient count, 0/negative = error
        // code (see Sms4FreeSmsSender for the mapping).
        public int? ProviderStatusCode { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        // Deterministic, not secret - a retried scheduler run for the same
        // (subscriber, scheduled_for) must compute the same key so the
        // insert conflicts on the unique index instead of sending twice.
        public static string ComputeIdempotencyKey(Guid subscriberId, DateTimeOffset scheduledFor)
        {
            string input = $"{subscriberId}:{scheduledFor:O}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexStringLower(hash);
        }
    }
}
