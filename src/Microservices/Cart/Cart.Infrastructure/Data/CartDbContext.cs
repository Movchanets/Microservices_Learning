using Cart.Domain.Aggregates;
using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Cart bounded context.
/// Manages ShoppingCart, CartItem, and ProductPrice entities.
/// ProductPrice is a local cache of Catalog pricing data, synced via integration events.
/// </summary>
public class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ShoppingCart>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.BuyerId);
            b.HasIndex(x => x.BuyerId)
             .IsUnique()
             .HasFilter("\"BuyerId\" IS NOT NULL");
            b.Property(x => x.Version).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(i => i.CartId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items)
             .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<CartItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CartId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.Property(x => x.StoreId).IsRequired();
            b.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        });

        builder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);
    }
}
