using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class SubscriberOtpCodeConfiguration : IEntityTypeConfiguration<SubscriberOtpCode>
    {
        public void Configure(EntityTypeBuilder<SubscriberOtpCode> builder)
        {
            builder.ToTable("subscriber_otp_codes", tb =>
            {
                tb.HasCheckConstraint("ck_subscriber_otp_codes_attempts", "attempts <= 3");
            });

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id)
                .ValueGeneratedNever();

            builder.Property(o => o.PhoneNumber)
                .IsRequired();

            builder.Property(o => o.CodeHash)
                .IsRequired();

            builder.Property(o => o.ExpiresAt)
                .IsRequired();

            builder.Property(o => o.Attempts)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(o => o.Used)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(o => o.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Supports both "find the latest unused, unexpired code for this
            // phone" (verify) and "count codes issued to this phone recently"
            // (abuse guard) lookups.
            builder.HasIndex(o => new { o.PhoneNumber, o.Used, o.ExpiresAt });
        }
    }
}
