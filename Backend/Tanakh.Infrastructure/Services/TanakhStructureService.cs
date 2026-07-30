using System.Collections.Generic;
using System.Linq;
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

        public List<BaseStructure> GetAll() => cacheProvider.GetTanakhStructureFromCache(CacheKey);

        public List<BaseStructure> GetBySection(string section) =>
            GetAll().Where(x => x.section?.ToLower() == section).ToList();

        public List<BaseStructure> GetByTitle(string title) =>
            GetAll().Where(x => x.title == title).ToList();
    }
}
