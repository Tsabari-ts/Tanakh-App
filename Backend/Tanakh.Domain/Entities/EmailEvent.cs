using System;

namespace Tanakh.Domain.Entities
{
    // Ingests provider webhooks (delivered/bounce/complaint/open). Immutable
    // once written - no created_at/updated_at, only occurred_at (provider's
    // timestamp) and received_at (ours). provider_event_id is unique so
    // re-posting the same webhook is a no-op, not a duplicate row.
    public class EmailEvent
    {
        public Guid Id { get; set; }

        public required string Provider { get; set; }

        public required string ProviderEventId { get; set; }

        public string? ProviderMessageId { get; set; }

        // Nullable - the event may arrive for an address we don't recognize.
        public Guid? SubscriberId { get; set; }

        public EmailEventType EventType { get; set; }

        public BounceType? BounceType { get; set; }

        // Raw provider payload, stored as jsonb.
        public required string Payload { get; set; }

        public DateTimeOffset OccurredAt { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }
    }
}
