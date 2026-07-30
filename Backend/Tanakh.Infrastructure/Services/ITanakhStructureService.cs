using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public interface ITanakhStructureService
    {
        Task<List<BaseStructure>> GetAllAsync(CancellationToken cancellationToken);

        Task<List<BaseStructure>> GetBySectionAsync(string section, CancellationToken cancellationToken);

        Task<List<BaseStructure>> GetByTitleAsync(string title, CancellationToken cancellationToken);
    }
}
