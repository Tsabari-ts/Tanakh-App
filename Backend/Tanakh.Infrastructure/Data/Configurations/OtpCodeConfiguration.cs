using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
    {
        public void Configure(EntityTypeBuilder<OtpCode> builder)
        {
            builder.ToTable("otp_codes", tb =>
            {
                tb.HasCheckConstraint("ck_otp_codes_attempts", "attempts <= 3");
            });

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id)
                .ValueGeneratedNever();

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

            // Supports "find the latest unused, unexpired OTP" lookup on verify.
            builder.HasIndex(o => new { o.Used, o.ExpiresAt });
        }
    }
}
