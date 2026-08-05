using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
    {
        public void Configure(EntityTypeBuilder<ErrorLog> builder)
        {
            builder.ToTable("error_log", tb =>
            {
                tb.HasCheckConstraint("ck_error_log_level", "level IN ('info','warn','error','fatal')");
            });

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.Level)
                .HasConversion(SnakeCaseEnumConverter<ErrorLevel>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Message)
                .IsRequired();

            builder.Property(e => e.Resolved)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Supports the default admin view (unresolved, error+fatal,
            // newest-first).
            builder.HasIndex(e => new { e.Resolved, e.Level, e.CreatedAt });
        }
    }
}
