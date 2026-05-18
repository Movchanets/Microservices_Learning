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
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        // Seed directly to DB
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-1", 2);
        cart.AddItem("SKU-2", 3);
        context.ShoppingCarts.Add(cart);
        await context.SaveChangesAsync();

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

        // Verify it's now in cache
        using var scope3 = _fixture.CreateScope();
        var repo3 = scope3.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedResult = await repo3.GetCartAsync(buyerId);
        cachedResult.BuyerId.Should().Be(buyerId);
        cachedResult.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrCreateTrackedCartAsync_NewCart_CreatesAndTracks()
    {
        // Arrange — mimics real flow: first add-to-cart for a new user
        var buyerId = $"buyer-new-{Guid.NewGuid():N}";

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);

        // Assert — cart exists in memory but not yet in DB
        cart.BuyerId.Should().Be(buyerId);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_FirstItem_PersistsToDbAndCache()
    {
        // Arrange — exact replica of AddCartItemCommandHandler flow
        var buyerId = $"buyer-first-add-{Guid.NewGuid():N}";

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — this is exactly what the handler does
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem("PROD-001", 2, 29.99m, "seller-1");
        await repo.SaveCartAsync(cart);

        // Assert — verify in DB directly
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().ContainSingle();
        dbCart.Items.First().Sku.Should().Be("PROD-001");
        dbCart.Items.First().Quantity.Should().Be(2);
        dbCart.Items.First().Price.Should().Be(29.99m);
        dbCart.Items.First().ShopId.Should().Be("seller-1");
        dbCart.Items.First().CartId.Should().Be(dbCart.Id);

        // Assert — verify in cache
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(buyerId);
        cachedCart.Items.Should().ContainSingle();
        cachedCart.Items.First().Sku.Should().Be("PROD-001");
    }

    [Fact]
    public async Task AddItem_SameSkuTwice_IncrementsQuantity()
    {
        // Arrange — mimics user adding same product twice
        var buyerId = $"buyer-same-sku-{Guid.NewGuid():N}";

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — first add
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem("PROD-001", 1, 10m);
        await repo.SaveCartAsync(cart);

        // Act — second add (new scope to simulate separate HTTP request)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(buyerId);
        cart2.AddItem("PROD-001", 3, 10m);
        await repo2.SaveCartAsync(cart2);

        // Assert — quantity should be 1 + 3 = 4
        var context = scope2.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().ContainSingle();
        dbCart.Items.First().Quantity.Should().Be(4);
    }

    [Fact]
    public async Task AddItem_DifferentSkus_KeepsBothItems()
    {
        // Arrange — mimics user adding different products
        var buyerId = $"buyer-multi-sku-{Guid.NewGuid():N}";

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — add first product
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem("PROD-A", 1, 10m, "seller-1");
        await repo.SaveCartAsync(cart);

        // Act — add second product (new scope)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(buyerId);
        cart2.AddItem("PROD-B", 2, 20m, "seller-2");
        await repo2.SaveCartAsync(cart2);

        // Assert — both items present
        var context = scope2.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().HaveCount(2);
        dbCart.Items.Should().Contain(i => i.Sku == "PROD-A" && i.Quantity == 1 && i.Price == 10m);
        dbCart.Items.Should().Contain(i => i.Sku == "PROD-B" && i.Quantity == 2 && i.Price == 20m);
    }

    [Fact]
    public async Task AddItem_ConcurrentRequests_NoConcurrencyException()
    {
        // Arrange — this is the exact scenario that caused DbUpdateConcurrencyException
        // Two concurrent requests adding different items to the same cart
        var buyerId = $"buyer-concurrent-{Guid.NewGuid():N}";

        // Seed initial cart
        using var setupScope = _fixture.CreateScope();
        var setupRepo = setupScope.ServiceProvider.GetRequiredService<CartRepository>();
        var setupCart = await setupRepo.GetOrCreateTrackedCartAsync(buyerId);
        setupCart.AddItem("INITIAL", 1, 5m);
        await setupRepo.SaveCartAsync(setupCart);

        // Act — two concurrent add-to-cart operations
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            using var scope = _fixture.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();
            var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
            cart.AddItem($"SKU-{i}", i, i * 10m, $"seller-{i}");
            await repo.SaveCartAsync(cart);
        });

        // Assert — should not throw
        var act = () => Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();

        // Verify final state
        using var verifyScope = _fixture.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CartDbContext>();
        verifyContext.ChangeTracker.Clear();

        var dbCart = await verifyContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        // At minimum: INITIAL + at least some of SKU-1..5
        dbCart!.Items.Should().Contain(i => i.Sku == "INITIAL");
        dbCart.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteCartAsync_RemovesFromDbAndCache()
    {
        // Arrange
        var buyerId = $"buyer-delete-{Guid.NewGuid():N}";
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem("SKU-D", 1);
        await repo.SaveCartAsync(cart);

        // Act
        await repo.DeleteCartAsync(buyerId);

        // Assert - DB should be empty
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();
        var dbCart = await context.ShoppingCarts.FirstOrDefaultAsync(c => c.BuyerId == buyerId);
        dbCart.Should().BeNull();

        // Assert - cache should return empty cart
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(buyerId);
        cachedCart.Items.Should().BeEmpty();
    }
}
