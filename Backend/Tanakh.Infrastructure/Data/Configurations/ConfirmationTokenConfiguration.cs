using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class ConfirmationTokenConfiguration : IEntityTypeConfiguration<ConfirmationToken>
    {
        public void Configure(EntityTypeBuilder<ConfirmationToken> builder)
        {
            builder.ToTable("confirmation_tokens", tb =>
            {
                tb.HasCheckConstraint("ck_confirmation_tokens_purpose", "purpose IN ('confirm')");
            });

            builder.HasKey(t => t.TokenHash);

            builder.Property(t => t.TokenHash)
                .ValueGeneratedNever();

            builder.Property(t => t.Purpose)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(t => t.ExpiresAt)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Supports "expire/cleanup tokens for this subscriber" lookups
            // (e.g. resend confirmation invalidates the previous token).
            builder.HasIndex(t => t.SubscriberId);

            builder.HasOne<Subscriber>()
                .WithMany()
                .HasForeignKey(t => t.SubscriberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
