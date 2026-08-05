using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class SubscriberAnonymizationService : ISubscriberAnonymizationService
    {
        private readonly AppDbContext dbContext;

        public SubscriberAnonymizationService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task AnonymizeAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        {
            Subscriber? subscriber = await dbContext.Subscribers
                .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

            if (subscriber is null)
            {
                return;
            }

            // NULL, not a tombstone string - the unique index on
            // phone_number allows multiple NULLs (Postgres default), so
            // this never collides with another row.
            subscriber.PhoneNumber = null;
            subscriber.DisplayName = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
