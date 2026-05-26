using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cart.IntegrationTests.Consumers;

[Collection("Cart collection")]
public class CatalogEventConsumerTests
{
    private readonly CartDatabaseFixture _fixture;

    public CatalogEventConsumerTests(CartDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── SkuCreatedConsumer ─────────────────────────────────────────────

    [Fact]
    public async Task SkuCreatedConsumer_ShouldCreateProductPriceInDb()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "INT-SKU-001",
            ProductName: "Test Product",
            StoreId: storeId,
            Price: 42.50m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string> { ["color"] = "red" },
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        using var scope = _fixture.CreateScope();
        var priceRepository = scope.ServiceProvider.GetRequiredService<IProductPriceRepository>();
        var consumer = new SkuCreatedConsumer(priceRepository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        var productPrice = await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SkuId == skuId);

        productPrice.Should().NotBeNull();
        productPrice!.SkuCode.Should().Be("INT-SKU-001");
        productPrice.Price.Should().Be(42.50m);
        productPrice.Currency.Should().Be("USD");
        productPrice.ProductId.Should().Be(productId);
        productPrice.StoreId.Should().Be(storeId);
    }

    [Fact]
    public async Task SkuCreatedConsumer_ShouldBeIdempotent_WhenDuplicateSkuId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "INT-IDEM-001",
            ProductName: "Test Product",
            StoreId: storeId,
            Price: 15.00m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act — consume the same event twice against the same DB
        using var scope = _fixture.CreateScope();
        var priceRepository = scope.ServiceProvider.GetRequiredService<IProductPriceRepository>();

        var consumer1 = new SkuCreatedConsumer(priceRepository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer1.Consume(consumeContext.Object);

        var consumer2 = new SkuCreatedConsumer(priceRepository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer2.Consume(consumeContext.Object);

        // Assert — only one record for this SKU
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        var count = await dbContext.ProductPrices.CountAsync(p => p.SkuId == skuId);
        count.Should().Be(1);

        var productPrice = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.SkuId == skuId);
        productPrice!.Price.Should().Be(15.00m);
    }

    [Fact]
    public async Task SkuCreatedConsumer_ShouldUpsertExistingProductPrice_WhenSkuAlreadyExists()
    {
        // Arrange — pre-create the product price in the cart DB
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, skuId, "INT-UPD-001", "Old Name", 10.00m, "USD", storeId));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "INT-UPD-001",
            ProductName: "Test Product",
            StoreId: storeId,
            Price: 25.99m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var priceRepository = scope.ServiceProvider.GetRequiredService<IProductPriceRepository>();
        var consumer = new SkuCreatedConsumer(priceRepository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SkuId == skuId);

        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(25.99m);
        productPrice.SkuCode.Should().Be("INT-UPD-001");
    }

    // ─── SkuDeletedConsumer ─────────────────────────────────────────────

    [Fact]
    public async Task SkuDeletedConsumer_ShouldRemoveProductPrice()
    {
        // Arrange — pre-create the product price
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, skuId, "INT-DEL-001", "To Delete", 30.00m, "USD", storeId));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var @event = new SkuDeletedEvent(productId, skuId, "INT-DEL-001", DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new SkuDeletedConsumer(dbContext, Mock.Of<ILogger<SkuDeletedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.SkuId == skuId);
        productPrice.Should().BeNull();
    }

    [Fact]
    public async Task SkuDeletedConsumer_ShouldNotThrow_WhenSkuNotFound()
    {
        // Arrange — SKU does not exist
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var @event = new SkuDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), "NONEXISTENT", DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert — should not throw
        var consumer = new SkuDeletedConsumer(dbContext, Mock.Of<ILogger<SkuDeletedConsumer>>());
        var act = () => consumer.Consume(consumeContext.Object);
        await act.Should().NotThrowAsync();
    }

    // ─── SkuPriceChangedConsumer ────────────────────────────────────────

    [Fact]
    public async Task SkuPriceChangedConsumer_ShouldUpdatePriceOnly()
    {
        // Arrange — pre-create the product price
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, skuId, "INT-PRC-001", "Price Change Product", 50.00m, "USD", storeId));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "INT-PRC-001",
            OldPrice: 50.00m,
            NewPrice: 75.00m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert — price updated, name unchanged
        var productPrice = await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SkuId == skuId);

        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(75.00m);
        productPrice.Currency.Should().Be("USD");
        productPrice.Name.Should().Be("Price Change Product");
    }

    [Fact]
    public async Task SkuPriceChangedConsumer_ShouldNotThrow_WhenSkuUnknown()
    {
        // Arrange — SKU does not exist
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var @event = new SkuPriceChangedEvent(
            ProductId: Guid.NewGuid(),
            SkuId: Guid.NewGuid(),
            SkuCode: "UNKNOWN-SKU",
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert — should not throw
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        var act = () => consumer.Consume(consumeContext.Object);
        await act.Should().NotThrowAsync();
    }
}
