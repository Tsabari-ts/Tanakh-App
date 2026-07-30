using System;

namespace Tanakh.Domain.Entities
{
    // Records sensitive actions (signup, unsubscribe, preferred-time change,
    // email change, admin actions) for who-did-what-when reconstruction.
    // Append-only, enforced by a DB trigger - see the AuditLog migration.
    // entity_id/entity_type are a loose, polymorphic reference (any entity
    // type), not a hard FK - actor is similarly loose text (a subscriber id,
    // "system", or an admin identity), not a FK to subscribers.
    public class AuditLogEntry
    {
        public Guid Id { get; set; }

        public required string Actor { get; set; }

        public required string Action { get; set; }

        public required string EntityType { get; set; }

        public Guid? EntityId { get; set; }

        public string? IpHash { get; set; }

        // jsonb, nullable.
        public string? Metadata { get; set; }

        public DateTimeOffset At { get; set; }
    }
}
