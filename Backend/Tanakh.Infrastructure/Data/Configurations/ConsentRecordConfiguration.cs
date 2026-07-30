using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
    {
        public void Configure(EntityTypeBuilder<ConsentRecord> builder)
        {
            builder.ToTable("consent_records", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_consent_records_consent_type",
                    "consent_type IN ('marketing','analytics','functional')");
            });

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.ConsentType)
                .HasConversion(SnakeCaseEnumConverter<ConsentType>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(c => c.IpHash)
                .IsRequired();

            builder.Property(c => c.UserAgent)
                .IsRequired();

            builder.Property(c => c.PolicyVersion)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(c => new { c.SubscriberId, c.ConsentType, c.GrantedAt });

            // Restrict, not Cascade: unlike reading_progress/reminder_deliveries,
            // consent_records is the legal proof of consent and must survive
            // even if a subscriber is later removed - the D-14/D-15
            // retention job anonymizes subscribers (keeps the row) rather
            // than hard-deleting them for exactly this reason.
            builder.HasOne<Subscriber>()
                .WithMany()
                .HasForeignKey(c => c.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
