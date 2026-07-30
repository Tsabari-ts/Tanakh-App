using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class ReadingProgressService : IReadingProgressService
    {
        private readonly AppDbContext dbContext;

        public ReadingProgressService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ReadingProgress>> GetProgressAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            return await dbContext.ReadingProgresses
                .Where(p => p.SubscriberId == subscriberId)
                .ToListAsync(cancellationToken);
        }

        public async Task UpsertProgressAsync(Guid subscriberId, ReadingSection section, string book, int chapter, int? verse, CancellationToken cancellationToken = default)
        {
            ReadingProgress? existing = await dbContext.ReadingProgresses
                .FirstOrDefaultAsync(p => p.SubscriberId == subscriberId && p.Section == section, cancellationToken);

            if (existing is null)
            {
                await dbContext.ReadingProgresses.AddAsync(new ReadingProgress
                {
                    Id = Guid.CreateVersion7(),
                    SubscriberId = subscriberId,
                    Section = section,
                    Book = book,
                    Chapter = chapter,
                    Verse = verse
                }, cancellationToken);
            }
            else
            {
                existing.Book = book;
                existing.Chapter = chapter;
                existing.Verse = verse;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
