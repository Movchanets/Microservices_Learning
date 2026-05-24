using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.StoreId)
            .IsRequired();

        builder.HasIndex(x => x.ProductId).IsUnique();

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AvailableQuantity)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRowVersion()
            .HasDefaultValueSql("gen_random_uuid()::text::bytea");
    }
}
