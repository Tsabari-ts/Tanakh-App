using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data.Conversions;

namespace Tanakh.Infrastructure.Data.Configurations
{
    public class ReadingProgressConfiguration : IEntityTypeConfiguration<ReadingProgress>
    {
        public void Configure(EntityTypeBuilder<ReadingProgress> builder)
        {
            builder.ToTable("reading_progress", tb =>
            {
                tb.HasCheckConstraint("ck_reading_progress_section", "section IN ('torah','neviim','ketuvim')");
                tb.HasCheckConstraint("ck_reading_progress_chapter", "chapter >= 1");
                tb.HasCheckConstraint("ck_reading_progress_verse", "verse IS NULL OR verse >= 1");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.Section)
                .HasConversion(SnakeCaseEnumConverter<ReadingSection>.Instance)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(p => p.Book)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.HasIndex(p => new { p.SubscriberId, p.Section })
                .IsUnique();

            builder.HasOne<Subscriber>()
                .WithMany()
                .HasForeignKey(p => p.SubscriberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
