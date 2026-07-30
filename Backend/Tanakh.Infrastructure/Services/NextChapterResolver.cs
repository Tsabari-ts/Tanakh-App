using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Model;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class NextChapterResolver : INextChapterResolver
    {
        private readonly IReadingProgressService readingProgressService;
        private readonly ITanakhStructureService structureService;
        private readonly RemindersOptions options;

        public NextChapterResolver(
            IReadingProgressService readingProgressService,
            ITanakhStructureService structureService,
            IOptions<RemindersOptions> options)
        {
            this.readingProgressService = readingProgressService;
            this.structureService = structureService;
            this.options = options.Value;
        }

        public async Task<NextChapterResult> ResolveAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            List<BaseStructure> books = await structureService.GetAllAsync(cancellationToken);

            IReadOnlyList<ReadingProgress> progress = await readingProgressService.GetProgressAsync(subscriberId, cancellationToken);
            ReadingProgress? current = progress.OrderByDescending(p => p.UpdatedAt).FirstOrDefault();

            if (current is null)
            {
                return DefaultStart(books);
            }

            int bookIndex = books.FindIndex(b => b.title == current.Book);
            if (bookIndex < 0)
            {
                // The book they were on no longer exists in the structure
                // data (unexpected, but data can change under us) - restart.
                return DefaultStart(books);
            }

            BaseStructure book = books[bookIndex];
            int chapterCount = book.chapters?.Count ?? book.length;

            if (current.Chapter < chapterCount)
            {
                return new NextChapterResult(book.section!, book.title!, current.Chapter + 1, false);
            }

            int nextIndex = bookIndex + 1;
            if (nextIndex >= books.Count)
            {
                return DefaultStart(books) with { CompletedCycle = true };
            }

            BaseStructure nextBook = books[nextIndex];
            return new NextChapterResult(nextBook.section!, nextBook.title!, 1, false);
        }

        private NextChapterResult DefaultStart(List<BaseStructure> books)
        {
            BaseStructure? defaultBook = books.FirstOrDefault(b => b.title == options.DefaultStartBook);
            return new NextChapterResult(
                defaultBook?.section ?? "Torah",
                options.DefaultStartBook,
                options.DefaultStartChapter,
                false);
        }
    }
}
