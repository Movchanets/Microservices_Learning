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
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever(); // Client-generated via Entity base class
            b.Property(x => x.BuyerId).IsRequired();
            b.HasIndex(x => x.BuyerId).IsUnique();

            // Map Version (uint) to PostgreSQL xmin system column.
            // IsRowVersion() on uint in Npgsql = automatic xmin concurrency token.
            // No physical column is created — xmin is a system column managed by PostgreSQL.
            b.Property(x => x.Version).IsRowVersion();

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasMany(x => x.Items)
             .WithOne()  // No back-reference — DDD aggregate children don't navigate to parent
             .HasForeignKey(i => i.CartId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever(); // Client-generated via Entity base class
            b.Property(x => x.CartId).IsRequired();
            b.Property(x => x.Sku).IsRequired().HasMaxLength(50);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.HasIndex(x => new { x.CartId, x.Sku }).IsUnique();
        });

        // ProductPrice configuration from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);
    }
}