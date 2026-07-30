using System.Collections.Generic;
using System.Linq;
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

        public Task<List<BaseStructure>> GetAllAsync() => cacheProvider.GetTanakhStructureFromCacheAsync(CacheKey);

        public async Task<List<BaseStructure>> GetBySectionAsync(string section) =>
            (await GetAllAsync()).Where(x => x.section?.ToLower() == section).ToList();

        public async Task<List<BaseStructure>> GetByTitleAsync(string title) =>
            (await GetAllAsync()).Where(x => x.title == title).ToList();
    }
}
