using Newtonsoft.Json;
using ServiceStack.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using Tanakh.Model;

namespace Tanakh.Controllers
{
    public class CacheProvider : IHttpHandler
    {
        private static readonly ObjectCache cache = MemoryCache.Default;

        public TanakhContainer GetFullTanakhFromCache(string cacheKey)
        {
            TanakhContainer tanakhContainer = new TanakhContainer();

            if (cache.Contains(cacheKey))
            {
                tanakhContainer = cache[cacheKey] as TanakhContainer;
            }
            else
            {
                string tanakhDataPath = "C:\\Users\\Tomer\\Desktop\\tomer\\myProjects\\Tanach in c#\\Tanakh\\Properties\\TanakhData.json";

                using (StreamReader reader = new StreamReader(tanakhDataPath))
                {
                    string jsonData = reader.ReadToEnd();
                    tanakhContainer = JsonConvert.DeserializeObject<TanakhContainer>(jsonData);

                    if (tanakhContainer != null)
                    {
                        PutInCache(cacheKey, tanakhContainer);
                    }
                }
            }

            return tanakhContainer;
        }

        private void PutInCache(string cacheKey, TanakhContainer tanakhContainer)
        {
            CacheItem cacheItem = new CacheItem(cacheKey, tanakhContainer);
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30)
            };

            cache.Add(cacheItem, cacheItemPolicy);
        }

        public List<BaseStructure> GetTanakhStructureFromCache(string cacheKey)
        {
            List<BaseStructure> books = new List<BaseStructure>();

            if (cache.Contains(cacheKey))
            {
                books = cache[cacheKey] as List<BaseStructure>;
            }
            else
            {
                string tanakhStructurePath = "C:\\Users\\Tomer\\Desktop\\tomer\\myProjects\\Tanach in c#\\Tanakh\\Properties\\TanakhStructure.json";

                using (StreamReader reader = new StreamReader(tanakhStructurePath))
                {
                    string jsonStructfure = reader.ReadToEnd();
                    books = JsonConvert.DeserializeObject<TanakhStructure>(jsonStructfure).Structures;

                    if (books.Any())
                    {
                        PutTanakhStructureInCache(cacheKey, books);
                    }
                }
            }

            return books;
        }

        private void PutTanakhStructureInCache(string cacheKey, List<BaseStructure> books)
        {
            CacheItem cacheItem = new CacheItem(cacheKey, books);
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) 
            };

            cache.Add(cacheItem, cacheItemPolicy);
        }
    }
}