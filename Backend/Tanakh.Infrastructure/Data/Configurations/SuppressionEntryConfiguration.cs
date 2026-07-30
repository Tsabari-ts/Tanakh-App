using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class SuppressionEntryConfiguration : IEntityTypeConfiguration<SuppressionEntry>
    {
        public void Configure(EntityTypeBuilder<SuppressionEntry> builder)
        {
            builder.ToTable("suppression_list", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_suppression_list_reason",
                    "reason IN ('hard_bounce','complaint','manual','unsubscribe')");
            });

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.EmailHash)
                .IsRequired();

            builder.HasIndex(e => e.EmailHash)
                .IsUnique();

            builder.Property(e => e.Reason)
                .HasConversion(SnakeCaseEnumConverter<SuppressionReason>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Source)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();
        }
    }
}
