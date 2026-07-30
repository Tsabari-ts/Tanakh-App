using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    // Section is the Tanakh-structure-data section name ("Torah"/"Prophets"/
    // "Writings"), i.e. the frontend's lowercased URL segment - not the
    // ReadingSection enum.
    public record NextChapterResult(string Section, string Book, int Chapter, bool CompletedCycle);

    public interface INextChapterResolver
    {
        // Resolution order: saved progress -> next chapter after it; no
        // progress -> the configured default start; end of a book -> the
        // next book in canonical order; end of the Tanakh -> back to the
        // default start with CompletedCycle = true.
        Task<NextChapterResult> ResolveAsync(Guid subscriberId, CancellationToken cancellationToken = default);
    }
}
