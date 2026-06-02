using System.Text.Json;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalog.Infrastructure.Persistence;

public sealed class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("Skus");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductId).IsRequired();
        builder.Property(s => s.SkuCode).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.ImageUrl).HasMaxLength(2000);

        // Money Value Object (Owned Type)
        builder.OwnsOne(s => s.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2);

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3);
        });

        // JSONB for attributes — requires explicit ValueConverter for Dictionary<string, string>
        var jsonConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, CatalogJsonOptions.Default),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, CatalogJsonOptions.Default) ?? new Dictionary<string, string>());

        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(k => k.Key, v => v.Value));

        builder.Property(s => s.TypedAttributes)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter, dictionaryComparer);

        builder.Property(s => s.FlexibleAttributes)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter, dictionaryComparer);

        // Relationship: SKU belongs to Product
        builder.HasOne<Product>()
            .WithMany(p => p.Skus)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.AttributeValues)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(s => s.SkuCode).IsUnique();
        builder.HasIndex(s => s.ProductId);
    }
}
