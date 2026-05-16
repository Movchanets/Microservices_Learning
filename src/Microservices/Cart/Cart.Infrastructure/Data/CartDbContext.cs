using Cart.Domain.Aggregates;
using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Data;

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
            b.HasKey(x => x.BuyerId);
            b.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey("ShoppingCartBuyerId") // Shadow foreign key
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(50);
        });

        // ProductPrice configuration from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);
    }
}