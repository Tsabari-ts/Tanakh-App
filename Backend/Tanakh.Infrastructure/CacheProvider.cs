using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Tanakh.Domain.Caching;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure
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
            if (cache.TryGet(cacheKey, out TanakhContainer? cached))
            {
                return cached;
            }

            string tanakhDataPath = Path.Combine(dataDirectory, "TanakhData.json");

            using (StreamReader reader = new StreamReader(tanakhDataPath))
            {
                string jsonData = reader.ReadToEnd();
                TanakhContainer? tanakhContainer = JsonSerializer.Deserialize<TanakhContainer>(jsonData);

                if (tanakhContainer?.structures is null)
                {
                    throw new InvalidOperationException(
                        $"Failed to parse Tanakh data file at '{tanakhDataPath}': missing or empty 'structures'.");
                }

                foreach (Structure structure in tanakhContainer.structures)
                {
                    if (structure.book is null || structure.sectionRef is null || structure.heTitle is null
                        || structure.heSectionRef is null || structure.he is null)
                    {
                        throw new InvalidOperationException(
                            $"Tanakh data file at '{tanakhDataPath}' has a section missing a required field " +
                            "(book/sectionRef/heTitle/heSectionRef/he).");
                    }
                }

                cache.Set(cacheKey, tanakhContainer);

                return tanakhContainer;
            }
        }

        public List<BaseStructure> GetTanakhStructureFromCache(string cacheKey)
        {
            if (cache.TryGet(cacheKey, out List<BaseStructure>? cached))
            {
                return cached;
            }

            string tanakhStructurePath = Path.Combine(dataDirectory, "TanakhStructure.json");

            using (StreamReader reader = new StreamReader(tanakhStructurePath))
            {
                string jsonStructfure = reader.ReadToEnd();
                List<BaseStructure>? books = JsonSerializer.Deserialize<TanakhStructure>(jsonStructfure)?.structures;

                if (books is null)
                {
                    throw new InvalidOperationException(
                        $"Failed to parse Tanakh structure file at '{tanakhStructurePath}': missing or empty 'structures'.");
                }

                foreach (BaseStructure item in books)
                {
                    if (item.section is null || item.title is null || item.book is null)
                    {
                        throw new InvalidOperationException(
                            $"Tanakh structure file at '{tanakhStructurePath}' has an entry missing a required field " +
                            "(section/title/book).");
                    }
                }

                if (books.Any())
                {
                    cache.Set(cacheKey, books);
                }

                return books;
            }
        }
    }
}
