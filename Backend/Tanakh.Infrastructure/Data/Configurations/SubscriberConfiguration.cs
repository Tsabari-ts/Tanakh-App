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
                    "status IN ('pending_confirmation','active','unsubscribed','bounced','complained')");
                tb.HasCheckConstraint(
                    "ck_subscribers_timezone_not_empty",
                    "length(trim(timezone)) > 0");
            });

            builder.HasKey(s => s.Id);

            // Generated in C# via Guid.CreateVersion7() before insert, not
            // by the database.
            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.Email)
                .HasColumnType("citext")
                .IsRequired();

            builder.HasIndex(s => s.Email)
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
