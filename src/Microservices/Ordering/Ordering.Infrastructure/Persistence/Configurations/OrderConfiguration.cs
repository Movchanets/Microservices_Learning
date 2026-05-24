using Ordering.Domain.Aggregates;
using Ordering.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BuyerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        // Address as owned type
        builder.OwnsOne(x => x.ShippingAddress, a =>
        {
            a.Property(p => p.Street).HasMaxLength(200).IsRequired();
            a.Property(p => p.City).HasMaxLength(100).IsRequired();
            a.Property(p => p.State).HasMaxLength(100);
            a.Property(p => p.Country).HasMaxLength(100).IsRequired();
            a.Property(p => p.ZipCode).HasMaxLength(20).IsRequired();
        });

        // OrderItems as owned collection
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.BuyerId);
    }
}
