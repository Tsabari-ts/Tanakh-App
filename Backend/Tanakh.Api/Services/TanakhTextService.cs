using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tanakh.Api.Model;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Model;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Api.Services
{
    public class TanakhTextService : ITanakhTextService
    {
        private readonly CacheProvider cacheProvider;
        private readonly ITanakhStructureService structureService;

        public TanakhTextService(CacheProvider cacheProvider, ITanakhStructureService structureService)
        {
            this.cacheProvider = cacheProvider;
            this.structureService = structureService;
        }

        public TanakhContext? GetChapter(string book, string chapter)
        {
            Dictionary<string, Book> dataDictionary = BuildChapterDictionary();
            string chosenSection = book + " " + chapter;

            if (!dataDictionary.TryGetValue(chosenSection, out Book? bookData))
            {
                return null;
            }

            return new TanakhContext
            {
                ChosenSection = chosenSection,
                BookData = bookData
            };
        }

        private Dictionary<string, Book> BuildChapterDictionary()
        {
            string cacheKey = "fullTanakh";
            TanakhContainer tanakhContainer = cacheProvider.GetFullTanakhFromCache(cacheKey);
            Dictionary<string, Book> dataDictionary = new Dictionary<string, Book>();

            List<BaseStructure> structureBooks = structureService.GetAll();
            int currentBookIndex = 1;

            // structures validated non-null in CacheProvider.GetFullTanakhFromCache
            foreach (Structure item in tanakhContainer.structures!)
            {
                string? nextSection = item.next;

                if (string.IsNullOrEmpty(nextSection))
                {
                    string nextBook = GetNextBookName(structureBooks, currentBookIndex);
                    currentBookIndex++;

                    if (!string.IsNullOrEmpty(nextBook))
                    {
                        nextSection = nextBook + " " + "1";
                    }
                }

                // book/sectionRef/heTitle/heSectionRef/he validated non-null in CacheProvider.GetFullTanakhFromCache
                string chosenSection = item.sectionRef!;
                string episodeData = string.Join(" ", item.he!);
                string verses = Regex.Replace(episodeData, @"<[^>]+>", "");

                Book bookEntry = new Book
                {
                    BookName = item.book!,
                    HebrewTitle = item.heTitle!,
                    HebrewSectionRef = item.heSectionRef!,
                    length = item.length,
                    NextChapter = nextSection,
                    PrevChapter = item.prev,
                    SectionRef = item.sectionRef!,
                    Verses = verses
                };

                dataDictionary.Add(chosenSection, bookEntry);
            }

            return dataDictionary;
        }

        private static string GetNextBookName(List<BaseStructure> books, int currentBookIndex)
        {
            string book = string.Empty;

            if (currentBookIndex < books.Count)
            {
                BaseStructure nextBook = books[currentBookIndex];
                book = nextBook.book ?? string.Empty;
            }

            return book;
        }
    }
}
