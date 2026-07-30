using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // One current position per (subscriber, section) - see the unique
    // constraint in ReadingProgressConfiguration. This is what produces the
    // deep link to "the chapter you were on" in the reminder email.
    public class ReadingProgress : IHasUpdatedAt
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }

        public ReadingSection Section { get; set; }

        public required string Book { get; set; }

        public int Chapter { get; set; }

        public int? Verse { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
