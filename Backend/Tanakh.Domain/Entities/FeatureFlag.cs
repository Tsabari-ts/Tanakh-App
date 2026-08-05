using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Unlike AppSetting, this is a genuinely growable list (admin adds/
    // removes named flags) rather than a fixed pair of singleton rows, so
    // it gets its own table with per-row CRUD instead of a JSON blob.
    public class FeatureFlag : IHasUpdatedAt
    {
        public required string Name { get; set; }

        public bool Enabled { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
