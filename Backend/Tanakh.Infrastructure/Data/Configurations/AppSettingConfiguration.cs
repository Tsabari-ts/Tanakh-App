using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            builder.ToTable("app_settings");

            builder.HasKey(s => s.Key);

            builder.Property(s => s.Key)
                .HasMaxLength(64);

            builder.Property(s => s.ValueJson)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .IsRequired();
        }
    }
}
