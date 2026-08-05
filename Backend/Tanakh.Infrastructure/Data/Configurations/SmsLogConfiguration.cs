using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class SmsLogConfiguration : IEntityTypeConfiguration<SmsLog>
    {
        public void Configure(EntityTypeBuilder<SmsLog> builder)
        {
            builder.ToTable("sms_log", tb =>
            {
                tb.HasCheckConstraint("ck_sms_log_type", "type IN ('reminder','otp','test')");
            });

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.ToPhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(s => s.Type)
                .HasConversion(SnakeCaseEnumConverter<SmsMessageType>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(s => s.Success)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Supports the log list's default (newest-first) ordering and
            // the stats/overview date-range queries.
            builder.HasIndex(s => s.CreatedAt)
                .IsDescending(true);
        }
    }
}
