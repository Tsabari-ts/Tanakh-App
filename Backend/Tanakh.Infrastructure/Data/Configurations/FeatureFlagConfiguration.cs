using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.ToTable("feature_flags");

            builder.HasKey(f => f.Name);

            builder.Property(f => f.Name)
                .HasMaxLength(64);

            builder.Property(f => f.Enabled)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(f => f.UpdatedAt)
                .IsRequired();
        }
    }
}
