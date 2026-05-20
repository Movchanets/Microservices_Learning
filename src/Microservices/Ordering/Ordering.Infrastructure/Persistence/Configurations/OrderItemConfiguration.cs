using Ordering.Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.StoreId)
            .IsRequired();

        builder.Ignore(x => x.TotalPrice);
    }
}
