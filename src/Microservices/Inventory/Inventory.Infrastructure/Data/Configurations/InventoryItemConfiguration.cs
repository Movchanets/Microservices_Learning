using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SkuId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.StoreId)
            .IsRequired();

        builder.Property(x => x.SkuCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AvailableQuantity)
            .IsRequired();

        builder.Property(x => x.ReservedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsDeactivated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Version)
            .IsRowVersion()
            .HasDefaultValueSql("gen_random_uuid()::text::bytea");

        // Indexes
        builder.HasIndex(x => x.SkuId).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.SkuCode);
    }
}
