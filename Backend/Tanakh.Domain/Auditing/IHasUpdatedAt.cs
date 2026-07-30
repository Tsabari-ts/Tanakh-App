using System;

namespace Tanakh.Domain.Auditing
{
    // Implemented by entities with an updated_at column, stamped on every
    // insert and update by AppDbContext.SaveChangesAsync.
    public interface IHasUpdatedAt
    {
        DateTimeOffset UpdatedAt { get; set; }
    }
}
