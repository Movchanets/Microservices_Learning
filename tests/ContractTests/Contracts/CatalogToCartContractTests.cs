using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Messaging.Consumers;
using Cart.Infrastructure.Repositories;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Catalog product lifecycle events
/// are correctly consumed by the Cart microservice.
///
/// These tests use EF Core InMemory provider for fast execution
/// without external infrastructure dependencies.
/// </summary>
public class CatalogToCartContractTests
{
    private CartDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CartDbContext(options);
    }

    private IProductPriceRepository CreateRepository(CartDbContext dbContext)
        => new ProductPriceRepository(dbContext);

    [Fact]
    public async Task SkuCreatedIntegrationEvent_Contract_ShouldCreateProductPriceInCart()
    {
        // Arrange - Catalog publishes SkuCreatedIntegrationEvent
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-001",
            ProductName: "Test Product",
            StoreId: Guid.NewGuid(),
            Price: 29.99m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);
        var consumer = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert - Cart should have a ProductPrice record
        var productPrice = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.SkuId == skuId);

        productPrice.Should().NotBeNull();
        productPrice!.SkuCode.Should().Be("SKU-001");
        productPrice.Name.Should().Be("Test Product");
        productPrice.Price.Should().Be(29.99m);
        productPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task SkuCreatedIntegrationEvent_Contract_ShouldBeIdempotent()
    {
        // Arrange - same SKU event sent twice (Catalog may replay)
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-IDEM-001",
            ProductName: "Idempotent Product",
            StoreId: Guid.NewGuid(),
            Price: 15.00m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act - consume twice with same DB context
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var consumer1 = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer1.Consume(consumeContext.Object);

        var consumer2 = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer2.Consume(consumeContext.Object);

        // Assert - only one record
        var count = await dbContext.ProductPrices.CountAsync(p => p.SkuId == skuId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldUpdateProductPriceInCart()
    {
        // Arrange - pre-create the product in Cart
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, skuId, "SKU-UPD-001", "Old Name", 10.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
        await dbContext.SaveChangesAsync();

        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-UPD-001",
            OldPrice: 10.00m,
            NewPrice: 19.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.SkuId == skuId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(19.99m);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldIgnoreUnknownSku()
    {
        // Arrange - SKU doesn't exist in Cart yet
        var @event = new SkuPriceChangedEvent(
            ProductId: Guid.NewGuid(),
            SkuId: Guid.NewGuid(),
            SkuCode: "SKU-UNKNOWN",
            OldPrice: 5.00m,
            NewPrice: 10.00m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act - should not throw
        await using var dbContext = CreateDbContext();
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert - no record created
        var count = await dbContext.ProductPrices.CountAsync();
        count.Should().Be(0, "unknown SKU should be logged and skipped");
    }

    [Fact]
    public async Task ProductDeletedEvent_Contract_ShouldRemoveProductPriceFromCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, Guid.NewGuid(), "SKU-DEL-001", "To Delete", 25.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
        await dbContext.SaveChangesAsync();

        var @event = new ProductDeletedEvent(productId, DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new ProductDeletedConsumer(dbContext, Mock.Of<ILogger<ProductDeletedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice.Should().BeNull();
    }

    [Fact]
    public async Task ProductDeletedEvent_Contract_ShouldBeIdempotentWhenNotFound()
    {
        // Arrange - product doesn't exist
        var @event = new ProductDeletedEvent(Guid.NewGuid(), DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert - should not throw
        await using var dbContext = CreateDbContext();
        var consumer = new ProductDeletedConsumer(dbContext, Mock.Of<ILogger<ProductDeletedConsumer>>());
        await consumer.Consume(consumeContext.Object);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldUpdatePriceForExistingSku()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, skuId, "SKU-PRICE-001", "Product", 10.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
        await dbContext.SaveChangesAsync();

        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-PRICE-001",
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.SkuId == skuId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(15.99m);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldIgnoreNonExistentSku()
    {
        // Arrange
        var @event = new SkuPriceChangedEvent(
            ProductId: Guid.NewGuid(),
            SkuId: Guid.NewGuid(),
            SkuCode: "SKU-NONEXISTENT",
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert - should not throw
        await using var dbContext = CreateDbContext();
        var consumer = new SkuPriceChangedConsumer(dbContext, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);
    }
}
