using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Required by Amendment 13 to the Israeli Privacy Protection Law: proof
    // of when and how consent was given. Append-only - revoking consent
    // means inserting a new row with Granted = false, never updating or
    // deleting an existing row. Enforced at the DB level by a trigger (see
    // the ConsentRecords migration), not just by convention.
    public class ConsentRecord : IHasCreatedAt
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }

        public ConsentType ConsentType { get; set; }

        public bool Granted { get; set; }

        public DateTimeOffset GrantedAt { get; set; }

        public required string IpHash { get; set; }

        public required string UserAgent { get; set; }

        public required string PolicyVersion { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
