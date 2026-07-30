using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class ReminderDeliveryConfiguration : IEntityTypeConfiguration<ReminderDelivery>
    {
        public void Configure(EntityTypeBuilder<ReminderDelivery> builder)
        {
            builder.ToTable("reminder_deliveries", tb =>
            {
                tb.HasCheckConstraint("ck_reminder_deliveries_status", "status IN ('pending','sent','failed','skipped')");
            });

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.Status)
                .HasConversion(SnakeCaseEnumConverter<DeliveryStatus>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(r => r.AttemptCount)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(r => r.IdempotencyKey)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            // Prevents duplicate sends at the DB level - a retried
            // scheduler run for the same subscriber+slot must conflict here.
            builder.HasIndex(r => new { r.SubscriberId, r.ScheduledFor })
                .IsUnique();

            builder.HasIndex(r => r.IdempotencyKey)
                .IsUnique();

            // Supports the dispatcher's "what's due" query.
            builder.HasIndex(r => new { r.Status, r.ScheduledFor });

            builder.HasOne<Subscriber>()
                .WithMany()
                .HasForeignKey(r => r.SubscriberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
