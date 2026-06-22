using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class SkuAttributeValueConfiguration : IEntityTypeConfiguration<SkuAttributeValue>
{
    public void Configure(EntityTypeBuilder<SkuAttributeValue> builder)
    {
        builder.ToTable("SkuAttributeValues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkuId).IsRequired();
        builder.Property(x => x.AttributeDefinitionId).IsRequired();
        builder.Property(x => x.Value).IsRequired().HasMaxLength(500);

        // Relationships
        builder.HasOne<Sku>()
            .WithMany(s => s.AttributeValues)
            .HasForeignKey(x => x.SkuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => new { x.SkuId, x.AttributeDefinitionId }).IsUnique();
    }
}
