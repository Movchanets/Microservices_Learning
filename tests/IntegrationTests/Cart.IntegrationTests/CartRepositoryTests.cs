using System.Text.Json;
using Cart.Domain.Aggregates;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cart.IntegrationTests;

[Collection("CartDatabaseCollection")]
public class CartRepositoryTests : IAsyncLifetime
{
    private readonly CartDatabaseFixture _fixture;
    private readonly CartDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly CartRepository _sut;

    public CartRepositoryTests(CartDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        _cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        _sut = new CartRepository(_cache, _dbContext);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _dbContext.ShoppingCarts.RemoveRange(_dbContext.ShoppingCarts);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCartAsync_CacheMiss_FetchesFromDbAndSetsInRedis()
    {
        // Arrange
        var buyerId = Guid.NewGuid().ToString();
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-1", 2);

        _dbContext.ShoppingCarts.Add(cart);
        await _dbContext.SaveChangesAsync();

        // Ensure redis is empty
        var cachedDataBefore = await _cache.GetStringAsync(buyerId);
        cachedDataBefore.Should().BeNull();

        // Act
        var result = await _sut.GetCartAsync(buyerId);

        // Assert
        result.Should().NotBeNull();
        result.BuyerId.Should().Be(buyerId);
        result.Items.Should().HaveCount(1);
        result.Items.First().Sku.Should().Be("SKU-1");

        // Verify redis was updated
        var cachedDataAfter = await _cache.GetStringAsync(buyerId);
        cachedDataAfter.Should().NotBeNull();

        var cachedCart = JsonSerializer.Deserialize<ShoppingCart>(cachedDataAfter!);
        cachedCart.Should().NotBeNull();
        cachedCart!.BuyerId.Should().Be(buyerId);
        cachedCart.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCartAsync_CacheHit_FetchesFromRedis()
    {
        // Arrange
        var buyerId = Guid.NewGuid().ToString();
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-CACHE", 5);

        var serializedCart = JsonSerializer.Serialize(cart);
        await _cache.SetStringAsync(buyerId, serializedCart);

        // DB does NOT have the cart

        // Act
        var result = await _sut.GetCartAsync(buyerId);

        // Assert
        result.Should().NotBeNull();
        result.BuyerId.Should().Be(buyerId);
        result.Items.Should().HaveCount(1);
        result.Items.First().Sku.Should().Be("SKU-CACHE");
        result.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCartAsync_UpdatesDbAndRedis()
    {
        // Arrange
        var buyerId = Guid.NewGuid().ToString();
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-INITIAL", 1);

        _dbContext.ShoppingCarts.Add(cart);
        await _dbContext.SaveChangesAsync();

        // Simulate modifications
        cart.UpdateQuantity("SKU-INITIAL", 2);
        cart.AddItem("SKU-NEW", 3);

        // Act
        await _sut.UpdateCartAsync(cart);

        // Assert DB
        _dbContext.ChangeTracker.Clear();
        var dbCart = await _sut.GetByIdAsync(cart.Id);
        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().HaveCount(2);

        // Assert Redis
        var cachedData = await _cache.GetStringAsync(buyerId);
        cachedData.Should().NotBeNull();

        var cachedCart = JsonSerializer.Deserialize<ShoppingCart>(cachedData!);
        cachedCart.Should().NotBeNull();
        cachedCart!.Items.Should().HaveCount(2);
        cachedCart.Items.Should().Contain(i => i.Sku == "SKU-INITIAL" && i.Quantity == 2);
        cachedCart.Items.Should().Contain(i => i.Sku == "SKU-NEW" && i.Quantity == 3);
    }

    [Fact]
    public async Task DeleteCartAsync_RemovesFromDbAndRedis()
    {
        // Arrange
        var buyerId = Guid.NewGuid().ToString();
        var cart = new ShoppingCart(buyerId);
        cart.AddItem("SKU-DEL", 1);

        _dbContext.ShoppingCarts.Add(cart);
        await _dbContext.SaveChangesAsync();
        await _cache.SetStringAsync(buyerId, JsonSerializer.Serialize(cart));

        // Act
        await _sut.DeleteCartAsync(buyerId);

        // Assert DB
        _dbContext.ChangeTracker.Clear();
        var dbCart = await _sut.GetByIdAsync(cart.Id);
        dbCart.Should().BeNull();

        // Assert Redis
        var cachedData = await _cache.GetStringAsync(buyerId);
        cachedData.Should().BeNull();
    }
}