using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Infrastructure.Services
{
    public class SuppressionService : ISuppressionService
    {
        private readonly AppDbContext dbContext;
        private readonly IHashingService hashingService;

        public SuppressionService(AppDbContext dbContext, IHashingService hashingService)
        {
            this.dbContext = dbContext;
            this.hashingService = hashingService;
        }

        public async Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default)
        {
            string emailHash = hashingService.HashEmail(email);

            return await dbContext.SuppressionEntries
                .AnyAsync(e => e.EmailHash == emailHash, cancellationToken);
        }
    }
}
