using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
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
        /// <param name="cancellationToken">Bound to the request's abort signal.</param>
        [HttpGet("books/{section}")]
        public async Task<IActionResult> GetBookListAsync(string section, CancellationToken cancellationToken)
        {
            return Ok(await structureService.GetBySectionAsync(section, cancellationToken));
        }

        /// <summary>Looks up structure entries for a single book by title.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        /// <param name="cancellationToken">Bound to the request's abort signal.</param>
        [HttpGet("books/main/{book}")]
        public async Task<IActionResult> getBookChapterAsync(string book, CancellationToken cancellationToken)
        {
            return Ok(await structureService.GetByTitleAsync(book, cancellationToken));
        }

        /// <summary>Returns the Hebrew text and navigation data for a single chapter.</summary>
        /// <param name="book">The book title (e.g. "Genesis").</param>
        /// <param name="chapter">The chapter number, as a string.</param>
        /// <param name="cancellationToken">Bound to the request's abort signal.</param>
        [HttpGet("books/{book}/{chapter}")]
        public async Task<IActionResult> GetChapterAsync(string book, string chapter, CancellationToken cancellationToken)
        {
            TanakhContext? context = await textService.GetChapterAsync(book, chapter, cancellationToken);

            if (context is null)
            {
                return NotFound();
            }

            return Ok(context);
        }
    }
}
