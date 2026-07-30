using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Api.Model;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Model;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Api.Controllers
{
    [Route("api/v1/reading-progress")]
    [ApiController]
    public class ReadingProgressController : ControllerBase
    {
        private readonly IReadingProgressService readingProgressService;
        private readonly ITanakhStructureService structureService;
        private readonly IUnsubscribeTokenService subscriberTokenService;

        public ReadingProgressController(
            IReadingProgressService readingProgressService,
            ITanakhStructureService structureService,
            IUnsubscribeTokenService subscriberTokenService)
        {
            this.readingProgressService = readingProgressService;
            this.structureService = structureService;
            this.subscriberTokenService = subscriberTokenService;
        }

        /// <summary>Records the chapter a known subscriber (identified via the signed token from their reminder email) just opened.</summary>
        [HttpPost]
        public async Task<IActionResult> UpsertAsync([FromBody] ReadingProgressRequest request, CancellationToken cancellationToken)
        {
            if (!subscriberTokenService.TryValidate(request.Token, out System.Guid subscriberId))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid token.");
            }

            List<BaseStructure> matches = await structureService.GetByTitleAsync(request.Book, cancellationToken);
            BaseStructure? book = matches.FirstOrDefault();

            if (book?.section is null)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown book.");
            }

            ReadingSection section = ReadingSectionMapper.FromStructureSectionName(book.section);

            await readingProgressService.UpsertProgressAsync(
                subscriberId, section, request.Book, request.Chapter, null, cancellationToken);

            return Ok();
        }
    }
}
