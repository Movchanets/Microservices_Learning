using Catalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(5000);
        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.StoreId).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Brand).HasMaxLength(100);

        // Tags as jsonb
        var listComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(p => p.Tags)
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(listComparer);

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Skus are configured via SkuConfiguration (HasOne<Product>.WithMany(p => p.Skus))
        // Ensure EF Core uses the backing field for change tracking
        builder.Navigation(p => p.Skus)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(p => p.AttributeValues)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(p => p.VariantAxes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.StoreId);
    }
}
