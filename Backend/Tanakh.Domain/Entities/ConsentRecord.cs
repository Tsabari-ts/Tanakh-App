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

        // Which version of each legal document (LEGAL_DOCS lastUpdated on
        // the frontend) and the exact consent sentence shown at signup -
        // required by Amendment 13 alongside PolicyVersion so a dispute can
        // be resolved against the precise wording the subscriber saw, not
        // just a single generic policy version.
        public required string TermsVersion { get; set; }

        public required string PrivacyVersion { get; set; }

        public required string ConsentText { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
