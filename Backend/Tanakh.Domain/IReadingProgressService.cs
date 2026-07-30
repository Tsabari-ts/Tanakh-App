using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain.Entities;

namespace Tanakh.Domain
{
    public interface IReadingProgressService
    {
        // Returns one entry per section the subscriber has ever read in
        // (at most 3 - Torah/Neviim/Ketuvim), not just one.
        Task<IReadOnlyList<ReadingProgress>> GetProgressAsync(Guid subscriberId, CancellationToken cancellationToken = default);

        Task UpsertProgressAsync(Guid subscriberId, ReadingSection section, string book, int chapter, int? verse, CancellationToken cancellationToken = default);
    }
}
