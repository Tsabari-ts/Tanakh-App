using Microsoft.AspNetCore.Mvc;
using Tanakh.Api.Model;
using Tanakh.Api.Services;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TanakhController : ControllerBase
    {
        private readonly ITanakhStructureService structureService;
        private readonly ITanakhTextService textService;

        public TanakhController(ITanakhStructureService structureService, ITanakhTextService textService)
        {
            this.structureService = structureService;
            this.textService = textService;
        }

        /// <summary>Lists the books belonging to a Tanakh section (e.g. "torah", "neviim", "ketuvim").</summary>
        /// <param name="section">The section name, case-insensitive.</param>
        [HttpGet("books/{section}")]
        public IActionResult GetBookList(string section)
        {
            return Ok(structureService.GetBySection(section));
        }

        /// <summary>Looks up structure entries for a single book by title.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        [HttpGet("books/main/{book}")]
        public IActionResult getBookChapter(string book)
        {
            return Ok(structureService.GetByTitle(book));
        }

        /// <summary>Returns the Hebrew text and navigation data for a single chapter.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        /// <param name="chapter">The chapter number, as a string.</param>
        [HttpGet("books/{book}/{chapter}")]
        public IActionResult GetChapter(string book, string chapter)
        {
            TanakhContext? context = textService.GetChapter(book, chapter);

            if (context is null)
            {
                return NotFound();
            }

            return Ok(context);
        }
    }
}
