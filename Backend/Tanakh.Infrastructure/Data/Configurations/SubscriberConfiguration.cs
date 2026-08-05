using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
    {
        public void Configure(EntityTypeBuilder<Subscriber> builder)
        {
            builder.ToTable("subscribers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_subscribers_status",
                    "status IN ('active','unsubscribed')");
                tb.HasCheckConstraint(
                    "ck_subscribers_timezone_not_empty",
                    "length(trim(timezone)) > 0");
                tb.HasCheckConstraint(
                    // An active subscriber must have a phone number to send
                    // reminders to; an anonymized (unsubscribed, past the
                    // retention window) row nulls it out - see
                    // SubscriberAnonymizationService.
                    "ck_subscribers_phone_required_when_active",
                    "status <> 'active' OR phone_number IS NOT NULL");
            });

            builder.HasKey(s => s.Id);

            // Generated in C# via Guid.CreateVersion7() before insert, not
            // by the database.
            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.PhoneNumber)
                .HasMaxLength(20);

            // Regular (non-partial) unique index - Postgres already treats
            // multiple NULLs as distinct, which is exactly what an
            // anonymized subscriber (phone_number = NULL) needs.
            builder.HasIndex(s => s.PhoneNumber)
                .IsUnique();

            builder.Property(s => s.Timezone)
                .HasDefaultValue("Asia/Jerusalem")
                .IsRequired();

            builder.Property(s => s.SkipShabbatHolidays)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(s => s.Locale)
                .HasDefaultValue("he-IL")
                .IsRequired();

            builder.Property(s => s.Status)
                .HasConversion(SnakeCaseEnumConverter<SubscriberStatus>.Instance)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .IsRequired();
        }
    }
}
