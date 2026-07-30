using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain.Auditing;

namespace Tanakh.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        // Pooled (AddDbContextPool) - constructor must take only
        // DbContextOptions, no injected scoped services.
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override int SaveChanges()
        {
            StampAuditTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampAuditTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void StampAuditTimestamps()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            foreach (EntityEntry<IHasCreatedAt> entry in ChangeTracker.Entries<IHasCreatedAt>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
            }

            foreach (EntityEntry<IHasUpdatedAt> entry in ChangeTracker.Entries<IHasUpdatedAt>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
