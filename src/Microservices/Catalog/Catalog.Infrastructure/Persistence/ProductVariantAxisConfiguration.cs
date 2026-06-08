using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class ProductVariantAxisConfiguration : IEntityTypeConfiguration<ProductVariantAxis>
{
    public void Configure(EntityTypeBuilder<ProductVariantAxis> builder)
    {
        builder.ToTable("ProductVariantAxes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.AttributeDefinitionId).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        // Relationships
        builder.HasOne<Product>()
            .WithMany(p => p.VariantAxes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => new { x.ProductId, x.AttributeDefinitionId }).IsUnique();
        builder.HasIndex(x => x.ProductId);
    }
}
