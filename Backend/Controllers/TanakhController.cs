using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tanakh.Model;

namespace Tanakh.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TanakhController : ControllerBase
    {
        private readonly CacheProvider cacheProvider;

        public TanakhController(CacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
        }

        /// <summary>Lists the books belonging to a Tanakh section (e.g. "torah", "neviim", "ketuvim").</summary>
        /// <param name="section">The section name, case-insensitive.</param>
        [HttpGet("books/{section}")]
        public IActionResult GetBookList(string section)
        {
            string cacheKey = "tanakhStructure";
            List<BaseStructure> books = cacheProvider.GetTanakhStructureFromCache(cacheKey);

            List<BaseStructure> relevantSections = books.Where(x => x.section?.ToLower() == section).ToList();

            return Ok(relevantSections);
        }

        /// <summary>Looks up structure entries for a single book by title.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        [HttpGet("books/main/{book}")]
        public IActionResult getBookChapter(string book)
        {
            string cacheKey = "tanakhStructure";
            List<BaseStructure> books = cacheProvider.GetTanakhStructureFromCache(cacheKey);

            List<BaseStructure> relevantSections = books.Where(x => x.title == book).ToList();

            return Ok(relevantSections);
        }

        /// <summary>Returns the Hebrew text and navigation data for a single chapter.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        /// <param name="chapter">The chapter number, as a string.</param>
        [HttpGet("books/{book}/{chapter}")]
        public IActionResult GetChapter(string book, string chapter)
        {
            Dictionary<string, Book> dataDictionary = Get();
            string chosenSection = book + " " + chapter;

            if (!dataDictionary.TryGetValue(chosenSection, out Book? bookData))
            {
                return NotFound();
            }

            TanakhContext context = new TanakhContext
            {
                ChosenSection = chosenSection,
                BookData = bookData
            };

            return Ok(context);
        }

        private Dictionary<string, Book> Get()
        {
            string cacheKey = "fullTanakh";
            TanakhContainer tanakhContainer = cacheProvider.GetFullTanakhFromCache(cacheKey);
            Dictionary<string, Book> dataDictionary = new Dictionary<string, Book>();

            int currentBookIndex = 1;

            // structures validated non-null in CacheProvider.GetFullTanakhFromCache
            foreach (Structure item in tanakhContainer.structures!)
            {
                string? nextSection = item.next;

                if (string.IsNullOrEmpty(nextSection))
                {
                    string nextBook = GetNextSection(currentBookIndex);
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

                Book testy = new Book
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

                dataDictionary.Add(chosenSection, testy);

            }

            return dataDictionary;
        }

        private string GetNextSection(int currentBookIndex)
        {
            string cacheKey = "tanakhStructure";
            List<BaseStructure> books = cacheProvider.GetTanakhStructureFromCache(cacheKey);

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
