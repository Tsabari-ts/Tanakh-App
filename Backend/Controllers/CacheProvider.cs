using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tanakh.Caching;
using Tanakh.Model;

namespace Tanakh.Controllers
{
    public class CacheProvider
    {
        private readonly ITanakhCache cache;
        private readonly string dataDirectory;

        public CacheProvider(ITanakhCache cache, IHostEnvironment environment, IConfiguration configuration)
        {
            this.cache = cache;
            dataDirectory = configuration["TanakhData:DataDirectory"]
                ?? Path.Combine(environment.ContentRootPath, "Data");
        }

        public TanakhContainer GetFullTanakhFromCache(string cacheKey)
        {
            if (cache.TryGet(cacheKey, out TanakhContainer cached))
            {
                return cached;
            }

            string tanakhDataPath = Path.Combine(dataDirectory, "TanakhData.json");

            using (StreamReader reader = new StreamReader(tanakhDataPath))
            {
                string jsonData = reader.ReadToEnd();
                TanakhContainer tanakhContainer = JsonConvert.DeserializeObject<TanakhContainer>(jsonData);

                if (tanakhContainer != null)
                {
                    cache.Set(cacheKey, tanakhContainer);
                }

                return tanakhContainer;
            }
        }

        public List<BaseStructure> GetTanakhStructureFromCache(string cacheKey)
        {
            if (cache.TryGet(cacheKey, out List<BaseStructure> cached))
            {
                return cached;
            }

            string tanakhStructurePath = Path.Combine(dataDirectory, "TanakhStructure.json");

            using (StreamReader reader = new StreamReader(tanakhStructurePath))
            {
                string jsonStructfure = reader.ReadToEnd();
                List<BaseStructure> books = JsonConvert.DeserializeObject<TanakhStructure>(jsonStructfure).Structures;

                if (books.Any())
                {
                    cache.Set(cacheKey, books);
                }

                return books;
            }
        }
    }
}
