using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Generic admin-editable key/value config - used for the two singleton
    // settings the admin panel exposes (Key = "maintenance" | "banner").
    // Not a general-purpose settings store beyond that; a real per-feature
    // config need should get its own typed Options class instead.
    public class AppSetting : IHasUpdatedAt
    {
        public required string Key { get; set; }

        public required string ValueJson { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
