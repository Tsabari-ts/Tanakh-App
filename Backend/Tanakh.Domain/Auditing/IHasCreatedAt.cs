using System;

namespace Tanakh.Domain.Auditing
{
    // Implemented by entities with a created_at column. Kept separate from
    // IHasUpdatedAt because not every audited table has both (e.g.
    // reading_progress only tracks updated_at).
    public interface IHasCreatedAt
    {
        DateTimeOffset CreatedAt { get; set; }
    }
}
