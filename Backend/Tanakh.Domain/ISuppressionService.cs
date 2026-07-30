using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public interface ISuppressionService
    {
        Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default);
    }
}
