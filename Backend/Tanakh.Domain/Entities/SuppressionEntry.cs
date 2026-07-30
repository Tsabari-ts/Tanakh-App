using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Global send block list - no email is ever sent to an address present
    // here, no exceptions (see ISuppressionService). email_hash is a keyed
    // (HMAC) hash, never the plaintext address - see IHashingService.
    // Append-only in practice: entries are added, never edited or removed.
    public class SuppressionEntry : IHasCreatedAt
    {
        public Guid Id { get; set; }

        public required string EmailHash { get; set; }

        public SuppressionReason Reason { get; set; }

        public required string Source { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string? Notes { get; set; }
    }
}
