using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain.Auditing;
using Tanakh.Domain.Entities;

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

        public DbSet<Subscriber> Subscribers => Set<Subscriber>();

        public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();

        public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();

        public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

        public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

        public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

        public DbSet<SmsLog> SmsLogs => Set<SmsLog>();

        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

        public DbSet<AppSetting> AppSettings => Set<AppSetting>();

        public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

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
