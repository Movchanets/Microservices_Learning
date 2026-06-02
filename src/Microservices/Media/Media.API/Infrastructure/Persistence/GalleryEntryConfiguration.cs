using Media.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Media.API.Infrastructure.Persistence;

public sealed class GalleryEntryConfiguration : IEntityTypeConfiguration<GalleryEntry>
{
    public void Configure(EntityTypeBuilder<GalleryEntry> builder)
    {
        builder.ToTable("GalleryEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MediaItemId)
            .IsRequired();

        builder.Property(x => x.TargetId)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SkuId);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Composite index for gallery lookups
        builder.HasIndex(x => new { x.TargetId, x.TargetType });

        // Index for reverse lookup (media item → gallery entry)
        builder.HasIndex(x => x.MediaItemId);
    }
}
