using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public class TanakhStructureService : ITanakhStructureService
    {
        private const string CacheKey = "tanakhStructure";

        private readonly CacheProvider cacheProvider;

        public TanakhStructureService(CacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
        }

        public Task<List<BaseStructure>> GetAllAsync(CancellationToken cancellationToken) =>
            cacheProvider.GetTanakhStructureFromCacheAsync(CacheKey, cancellationToken);

        public async Task<List<BaseStructure>> GetBySectionAsync(string section, CancellationToken cancellationToken) =>
            (await GetAllAsync(cancellationToken)).Where(x => x.section?.ToLower() == section).ToList();

        public async Task<List<BaseStructure>> GetByTitleAsync(string title, CancellationToken cancellationToken) =>
            (await GetAllAsync(cancellationToken)).Where(x => x.title == title).ToList();
    }
}
