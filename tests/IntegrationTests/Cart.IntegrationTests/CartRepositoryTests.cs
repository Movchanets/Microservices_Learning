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
    private static readonly Guid StoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public CartRepositoryTests(CartDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCartAsync_CacheMiss_LoadsFromDbAndCaches()
    {
        // Arrange
        Guid? buyerId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        // Seed directly to DB
        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();
        var cart = new ShoppingCart(buyerId);
        cart.AddItem(prod1Id, 2, StoreId);
        cart.AddItem(prod2Id, 3, StoreId);
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
        result.Items.Should().Contain(i => i.ProductId == prod1Id && i.Quantity == 2);
        result.Items.Should().Contain(i => i.ProductId == prod2Id && i.Quantity == 3);

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
        Guid? buyerId = Guid.NewGuid();

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
        Guid? buyerId = Guid.NewGuid();
        var prodId = Guid.NewGuid();

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — this is exactly what the handler does
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem(prodId, 2, StoreId, 29.99m);
        await repo.SaveCartAsync(cart);

        // Assert — verify in DB directly
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().ContainSingle();
        dbCart.Items.First().ProductId.Should().Be(prodId);
        dbCart.Items.First().Quantity.Should().Be(2);
        dbCart.Items.First().Price.Should().Be(29.99m);
        dbCart.Items.First().StoreId.Should().Be(StoreId);
        dbCart.Items.First().CartId.Should().Be(dbCart.Id);

        // Assert — verify in cache
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(buyerId);
        cachedCart.Items.Should().ContainSingle();
        cachedCart.Items.First().ProductId.Should().Be(prodId);
    }

    [Fact]
    public async Task AddItem_SameSkuTwice_IncrementsQuantity()
    {
        // Arrange — mimics user adding same product twice
        Guid? buyerId = Guid.NewGuid();
        var prodId = Guid.NewGuid();

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — first add
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem(prodId, 1, StoreId, 10m);
        await repo.SaveCartAsync(cart);

        // Act — second add (new scope to simulate separate HTTP request)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(buyerId);
        cart2.AddItem(prodId, 3, StoreId, 10m);
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
        Guid? buyerId = Guid.NewGuid();
        var prodAId = Guid.NewGuid();
        var prodBId = Guid.NewGuid();

        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — add first product
        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem(prodAId, 1, StoreId, 10m);
        await repo.SaveCartAsync(cart);

        // Act — add second product (new scope)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(buyerId);
        cart2.AddItem(prodBId, 2, StoreId, 20m);
        await repo2.SaveCartAsync(cart2);

        // Assert — both items present
        var context = scope2.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();

        var dbCart = await context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().HaveCount(2);
        dbCart.Items.Should().Contain(i => i.ProductId == prodAId && i.Quantity == 1 && i.Price == 10m);
        dbCart.Items.Should().Contain(i => i.ProductId == prodBId && i.Quantity == 2 && i.Price == 20m);
    }

    [Fact]
    public async Task DeleteCartAsync_RemovesFromDbAndCache()
    {
        // Arrange
        Guid? buyerId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
        cart.AddItem(Guid.NewGuid(), 1, StoreId);
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

    [Fact]
    public async Task SaveCartAsync_OnConcurrencyException_InvalidatesCacheAndThrows()
    {
        // Arrange
        Guid? buyerId = Guid.NewGuid();
        var prodInitId = Guid.NewGuid();
        var prodAId = Guid.NewGuid();
        var prodBId = Guid.NewGuid();

        // Setup: create and save initial cart
        using (var setupScope = _fixture.CreateScope())
        {
            var repo = setupScope.ServiceProvider.GetRequiredService<CartRepository>();
            var cart = await repo.GetOrCreateTrackedCartAsync(buyerId);
            cart.AddItem(prodInitId, 1, StoreId, 10m);
            await repo.SaveCartAsync(cart);
        }

        // Scope 1 loads cart
        using var scope1 = _fixture.CreateScope();
        var repo1 = scope1.ServiceProvider.GetRequiredService<CartRepository>();
        var cart1 = await repo1.GetOrCreateTrackedCartAsync(buyerId);

        // Scope 2 loads cart (stale relative to scope 1's future changes)
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(buyerId);

        // Scope 1 updates and saves successfully
        cart1.AddItem(prodAId, 1, StoreId, 20m);
        await repo1.SaveCartAsync(cart1);

        // Scope 2 updates and tries to save (should throw DbUpdateConcurrencyException)
        cart2.AddItem(prodBId, 1, StoreId, 30m);

        Func<Task> act = async () => await repo2.SaveCartAsync(cart2);

        // Assert - throws concurrency exception
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Assert - cache is evicted/invalidated, so GetCartAsync fetches the latest database state (scope 1's state)
        using var checkScope = _fixture.CreateScope();
        var checkRepo = checkScope.ServiceProvider.GetRequiredService<CartRepository>();
        var finalCart = await checkRepo.GetCartAsync(buyerId);

        finalCart.Should().NotBeNull();
        finalCart.Items.Should().HaveCount(2); // PROD-INIT and PROD-A
        finalCart.Items.Should().Contain(i => i.ProductId == prodAId);
        finalCart.Items.Should().NotContain(i => i.ProductId == prodBId);
    }

    [Fact]
    public async Task AnonymousCart_NoBuyerId_UsesCartIdAsKey()
    {
        // Arrange — anonymous user, no BuyerId
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — create anonymous cart
        var cart = await repo.GetOrCreateTrackedCartAsync(null);
        var prodId = Guid.NewGuid();
        cart.AddItem(prodId, 1, StoreId, 15m);
        await repo.SaveCartAsync(cart);

        // Assert — BuyerId is null, cart has items
        cart.BuyerId.Should().BeNull();
        cart.Items.Should().ContainSingle();

        // Act — retrieve anonymous cart by cartId
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var retrieved = await repo2.GetCartAsync(null, cart.Id);

        // Assert
        retrieved.BuyerId.Should().BeNull();
        retrieved.Items.Should().ContainSingle();
        retrieved.Items.First().ProductId.Should().Be(prodId);
    }

    [Fact]
    public async Task AnonymousCart_MultipleItems_PersistsCorrectly()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — add multiple items to anonymous cart
        var cart = await repo.GetOrCreateTrackedCartAsync(null);
        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();
        var prod3Id = Guid.NewGuid();
        cart.AddItem(prod1Id, 1, StoreId, 10m);
        cart.AddItem(prod2Id, 2, StoreId, 20m);
        cart.AddItem(prod3Id, 3, StoreId, 30m);
        await repo.SaveCartAsync(cart);

        // Assert — retrieve by cartId
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var retrieved = await repo2.GetCartAsync(null, cart.Id);

        retrieved.Items.Should().HaveCount(3);
        retrieved.Items.Should().Contain(i => i.ProductId == prod1Id && i.Quantity == 1);
        retrieved.Items.Should().Contain(i => i.ProductId == prod2Id && i.Quantity == 2);
        retrieved.Items.Should().Contain(i => i.ProductId == prod3Id && i.Quantity == 3);
    }

    [Fact]
    public async Task AnonymousCart_DeleteByCartId_RemovesFromDbAndCache()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = await repo.GetOrCreateTrackedCartAsync(null);
        cart.AddItem(Guid.NewGuid(), 1, StoreId, 10m);
        await repo.SaveCartAsync(cart);
        var cartId = cart.Id;

        // Act — delete by cartId
        await repo.DeleteCartAsync(null, cartId);

        // Assert — DB should be empty
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        context.ChangeTracker.Clear();
        var dbCart = await context.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);
        dbCart.Should().BeNull();

        // Assert — cache should return empty cart
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cachedCart = await repo2.GetCartAsync(null, cartId);
        cachedCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AnonymousCart_MultipleAnonCarts_CanCoexist()
    {
        // Arrange — two separate anonymous users
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — create two anonymous carts
        var cart1 = await repo.GetOrCreateTrackedCartAsync(null);
        var prod1Id = Guid.NewGuid();
        cart1.AddItem(prod1Id, 1, StoreId, 10m);
        await repo.SaveCartAsync(cart1);

        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(null);
        var prod2Id = Guid.NewGuid();
        cart2.AddItem(prod2Id, 2, StoreId, 20m);
        await repo2.SaveCartAsync(cart2);

        // Assert — both carts exist independently
        cart1.Id.Should().NotBe(cart2.Id);
        cart1.BuyerId.Should().BeNull();
        cart2.BuyerId.Should().BeNull();

        // Verify each is retrievable by its own cartId
        using var scope3 = _fixture.CreateScope();
        var repo3 = scope3.ServiceProvider.GetRequiredService<CartRepository>();
        var retrieved1 = await repo3.GetCartAsync(null, cart1.Id);
        var retrieved2 = await repo3.GetCartAsync(null, cart2.Id);

        retrieved1.Items.Should().ContainSingle();
        retrieved1.Items.First().ProductId.Should().Be(prod1Id);
        retrieved2.Items.Should().ContainSingle();
        retrieved2.Items.First().ProductId.Should().Be(prod2Id);
    }

    [Fact]
    public async Task AnonymousCart_UpdateQuantity_WorksCorrectly()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        var cart = await repo.GetOrCreateTrackedCartAsync(null);
        var prodId = Guid.NewGuid();
        cart.AddItem(prodId, 1, StoreId, 10m);
        await repo.SaveCartAsync(cart);

        // Act — update quantity in new scope
        using var scope2 = _fixture.CreateScope();
        var repo2 = scope2.ServiceProvider.GetRequiredService<CartRepository>();
        var cart2 = await repo2.GetOrCreateTrackedCartAsync(null, cart.Id);
        cart2.UpdateQuantity(prodId, 5);
        await repo2.SaveCartAsync(cart2);

        // Assert
        using var scope3 = _fixture.CreateScope();
        var repo3 = scope3.ServiceProvider.GetRequiredService<CartRepository>();
        var retrieved = await repo3.GetCartAsync(null, cart.Id);
        retrieved.Items.Should().ContainSingle();
        retrieved.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AnonymousCart_GetCartAsync_NoBuyerIdNoCartId_ReturnsEmptyCart()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<CartRepository>();

        // Act — no identity at all
        var cart = await repo.GetCartAsync(null, null);

        // Assert — returns empty transient cart
        cart.BuyerId.Should().BeNull();
        cart.Items.Should().BeEmpty();
    }
}
