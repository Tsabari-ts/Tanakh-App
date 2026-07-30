using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public interface ISubscriberAnonymizationService
    {
        // Replaces email with a tombstone value and nulls display_name;
        // keeps the row itself (for aggregate statistics) rather than
        // deleting it - see docs/privacy.md.
        Task AnonymizeAsync(Guid subscriberId, CancellationToken cancellationToken = default);
    }
}
