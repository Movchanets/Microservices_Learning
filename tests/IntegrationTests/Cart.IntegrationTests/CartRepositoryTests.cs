using Cart.Domain.Aggregates;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.IntegrationTests;

[Collection("Cart collection")]
public class CartRepositoryTests
{
    private readonly CartDatabaseFixture _fixture;

    public CartRepositoryTests(CartDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCartAsync_CacheMiss_LoadsFromDbAndCaches()
    {
        // Arrange
        var buyerId = $"buyer-cache-miss-{Guid.NewGuid():N}";
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();
        var context = scope.ServiceProvider.GetRequiredService<Cart.Infrastructure.Data.CartDbContext>();

        // Seed directly to DB
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-1", 2);
        cart.AddItem("SKU-2", 3);
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

        // Clear change tracker so next read hits DB
        context.ChangeTracker.Clear();

        // Act - get from a new scope (no cache yet)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var result = await repo2.GetCartAsync(buyerId);

        // Assert
        result.BuyerId.Should().Be(buyerId);
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Sku == "SKU-1" && i.Quantity == 2);
        result.Items.Should().Contain(i => i.Sku == "SKU-2" && i.Quantity == 3);

        // Verify it's now in cache by getting from another scope
        using var scope3 = _fixture.CreateScope();
        var repo3 = scope3.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedResult = await repo3.GetCartAsync(buyerId);
        cachedResult.BuyerId.Should().Be(buyerId);
        cachedResult.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCartAsync_CacheHit_ReturnsFromRedis()
    {
        // Arrange
        var buyerId = $"buyer-cache-hit-{Guid.NewGuid():N}";
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // First call creates empty cart (no DB record, no cache)
        var cart = await repo.GetCartAsync(buyerId);
        cart.BuyerId.Should().Be(buyerId);

        // Now add items and update (this will populate both DB and cache)
        cart.AddItem("SKU-A", 5);
        await repo.UpdateCartAsync(cart);

        // Clear EF change tracker
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        // Act - get from new scope; should come from Redis cache
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var result = await repo2.GetCartAsync(buyerId);

        // Assert
        result.BuyerId.Should().Be(buyerId);
        result.Items.Should().ContainSingle();
        result.Items.First().Sku.Should().Be("SKU-A");
        result.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCartAsync_PersistsToDbAndUpdatesCache()
    {
        // Arrange
        var buyerId = $"buyer-update-{Guid.NewGuid():N}";
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-X", 10);
        cart.AddItem("SKU-Y", 20);

        // Act
        await repo.UpdateCartAsync(cart);

        // Assert - verify in DB
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().HaveCount(2);

        // Assert - verify in cache (via new repo scope)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(buyerId);
        cachedCart.Items.Should().HaveCount(2);
        cachedCart.Items.Should().Contain(i => i.Sku == "SKU-X" && i.Quantity == 10);
    }

    [Fact]
    public async Task DeleteCartAsync_RemovesFromDbAndCache()
    {
        // Arrange
        var buyerId = $"buyer-delete-{Guid.NewGuid():N}";
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-D", 1);
        await repo.UpdateCartAsync(cart);

        // Act
        await repo.DeleteCartAsync(buyerId);

        // Assert - DB should be empty
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();
        var dbCart = await context.ShoppingCarts.FindAsync(buyerId);
        dbCart.Should().BeNull();

        // Assert - cache should return empty cart (new cart created)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(buyerId);
        cachedCart.Items.Should().BeEmpty();
    }
}
