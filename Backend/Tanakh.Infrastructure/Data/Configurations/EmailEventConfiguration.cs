using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class EmailEventConfiguration : IEntityTypeConfiguration<EmailEvent>
    {
        public void Configure(EntityTypeBuilder<EmailEvent> builder)
        {
            builder.ToTable("email_events", tb =>
            {
                tb.HasCheckConstraint("ck_email_events_event_type", "event_type IN ('delivered','bounce','complaint','open')");
                tb.HasCheckConstraint("ck_email_events_bounce_type", "bounce_type IS NULL OR bounce_type IN ('hard','soft')");
            });

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.Provider)
                .IsRequired();

            builder.Property(e => e.ProviderEventId)
                .IsRequired();

            builder.HasIndex(e => e.ProviderEventId)
                .IsUnique();

            builder.HasIndex(e => e.ProviderMessageId);

            builder.Property(e => e.EventType)
                .HasConversion(SnakeCaseEnumConverter<EmailEventType>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.BounceType)
                .HasConversion(SnakeCaseEnumConverter<BounceType>.NullableInstance)
                .HasMaxLength(8);

            builder.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.HasOne<Subscriber>()
                .WithMany()
                .HasForeignKey(e => e.SubscriberId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
