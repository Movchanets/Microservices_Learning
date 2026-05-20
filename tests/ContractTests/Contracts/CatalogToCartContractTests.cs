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
    public async Task ProductCreatedEvent_Contract_ShouldCreateProductPriceInCart()
    {
        // Arrange - Catalog publishes this event shape
        var productId = Guid.NewGuid();
        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Test Product",
            Description: "A test product",
            Price: 29.99m,
            Currency: "USD",
            Sku: "SKU-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Electronics",
            Tags: ["test", "electronics"],
            ImageUrl: "https://example.com/image.jpg",
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Brand: "TestBrand",
            Attributes: new Dictionary<string, string> { ["color"] = "blue" });

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);
        var consumer = new ProductCreatedConsumer(repository, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert - Cart should have a ProductPrice record
        var productPrice = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId);

        productPrice.Should().NotBeNull();
        productPrice!.Sku.Should().Be("SKU-001");
        productPrice.Name.Should().Be("Test Product");
        productPrice.Price.Should().Be(29.99m);
        productPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ProductCreatedEvent_Contract_ShouldBeIdempotent()
    {
        // Arrange - same product sent twice (Catalog may replay)
        var productId = Guid.NewGuid();
        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Idempotent Product",
            Description: "Desc",
            Price: 15.00m,
            Currency: "USD",
            Sku: "SKU-IDEM-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Test",
            Tags: [],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act - consume twice with same DB context
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var consumer1 = new ProductCreatedConsumer(repository, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer1.Consume(consumeContext.Object);

        var consumer2 = new ProductCreatedConsumer(repository, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer2.Consume(consumeContext.Object);

        // Assert - only one record
        var count = await dbContext.ProductPrices.CountAsync(p => p.Id == productId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ProductUpdatedEvent_Contract_ShouldUpdateProductPriceInCart()
    {
        // Arrange - pre-create the product in Cart
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "SKU-UPD-001", "Old Name", 10.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
        await dbContext.SaveChangesAsync();

        var @event = new ProductUpdatedEvent(
            ProductId: productId,
            Name: "Updated Product",
            Description: "Updated desc",
            Price: 19.99m,
            Currency: "USD",
            Sku: "SKU-UPD-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Updated Category",
            Tags: ["updated"],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            IsActive: true,
            UpdatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var repository = CreateRepository(dbContext);
        var consumer = new ProductUpdatedConsumer(repository, Mock.Of<ILogger<ProductUpdatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(19.99m);
        productPrice.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task ProductUpdatedEvent_Contract_ShouldCreateIfNotExists()
    {
        // Arrange - product doesn't exist in Cart yet (event ordering issue)
        var productId = Guid.NewGuid();
        var @event = new ProductUpdatedEvent(
            ProductId: productId,
            Name: "New From Update",
            Description: "Desc",
            Price: 5.00m,
            Currency: "USD",
            Sku: "SKU-NEWUPD-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Cat",
            Tags: [],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            IsActive: true,
            UpdatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);
        var consumer = new ProductUpdatedConsumer(repository, Mock.Of<ILogger<ProductUpdatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert - should be created as fallback
        var productPrice = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Sku == "SKU-NEWUPD-001");
        productPrice.Should().NotBeNull();
        productPrice!.Name.Should().Be("New From Update");
    }

    [Fact]
    public async Task ProductDeletedEvent_Contract_ShouldRemoveProductPriceFromCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "SKU-DEL-001", "To Delete", 25.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
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
    public async Task ProductPriceChangedEvent_Contract_ShouldUpdatePriceInCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "SKU-PRICE-001", "Product", 10.00m, "USD", Guid.Parse("33333333-3333-3333-3333-333333333333")));
        await dbContext.SaveChangesAsync();

        var @event = new ProductPriceChangedEvent(
            ProductId: productId,
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new ProductPriceChangedConsumer(dbContext, Mock.Of<ILogger<ProductPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(15.99m);
    }

    [Fact]
    public async Task ProductPriceChangedEvent_Contract_ShouldIgnoreUnknownProduct()
    {
        // Arrange
        var @event = new ProductPriceChangedEvent(
            ProductId: Guid.NewGuid(),
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert - should not throw
        await using var dbContext = CreateDbContext();
        var consumer = new ProductPriceChangedConsumer(dbContext, Mock.Of<ILogger<ProductPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);
    }
}
