using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
    {
        public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
        {
            builder.ToTable("audit_log");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .ValueGeneratedNever();

            builder.Property(a => a.Actor)
                .IsRequired();

            builder.Property(a => a.Action)
                .IsRequired();

            builder.Property(a => a.EntityType)
                .IsRequired();

            builder.Property(a => a.Metadata)
                .HasColumnType("jsonb");

            builder.Property(a => a.At)
                .IsRequired();

            builder.HasIndex(a => new { a.EntityId, a.At })
                .IsDescending(false, true);

            builder.HasIndex(a => a.At)
                .IsDescending(true);
        }
    }
}
