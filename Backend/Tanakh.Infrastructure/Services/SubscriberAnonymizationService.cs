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

            // Still unique (citext + the subscriber's own id) so the
            // unique index on email never rejects an anonymize operation.
            subscriber.Email = $"deleted-{subscriber.Id}@anonymized.invalid";
            subscriber.DisplayName = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
