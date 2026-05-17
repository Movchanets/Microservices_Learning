using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
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

    // ─── ProductCreatedConsumer ───────────────────────────────────────────

    [Fact]
    public async Task ProductCreatedConsumer_ShouldCreateProductPriceInDb()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Integration Test Product",
            Description: "A product for integration testing",
            Price: 42.50m,
            Currency: "USD",
            Sku: "INT-SKU-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Test Category",
            Tags: ["test", "integration"],
            ImageUrl: "https://example.com/img.jpg",
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Brand: "TestBrand",
            Attributes: new Dictionary<string, string> { ["color"] = "red" });

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        var consumer = new ProductCreatedConsumer(dbContext, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId);

        productPrice.Should().NotBeNull();
        productPrice!.Sku.Should().Be("INT-SKU-001");
        productPrice.Name.Should().Be("Integration Test Product");
        productPrice.Price.Should().Be(42.50m);
        productPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ProductCreatedConsumer_ShouldBeIdempotent_WhenDuplicateProductId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Idempotent Product",
            Description: "Desc",
            Price: 15.00m,
            Currency: "USD",
            Sku: "INT-IDEM-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Test",
            Tags: [],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act — consume the same event twice against the same DB
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var consumer1 = new ProductCreatedConsumer(dbContext, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer1.Consume(consumeContext.Object);

        var consumer2 = new ProductCreatedConsumer(dbContext, Mock.Of<ILogger<ProductCreatedConsumer>>());
        await consumer2.Consume(consumeContext.Object);

        // Assert — only one record for this product
        var count = await dbContext.ProductPrices.CountAsync(p => p.Id == productId);
        count.Should().Be(1);

        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice!.Price.Should().Be(15.00m);
    }

    // ─── ProductUpdatedConsumer ───────────────────────────────────────────

    [Fact]
    public async Task ProductUpdatedConsumer_ShouldUpdateExistingProductPrice()
    {
        // Arrange — pre-create the product in the cart DB
        var productId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "INT-UPD-001", "Old Name", 10.00m, "USD"));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var @event = new ProductUpdatedEvent(
            ProductId: productId,
            Name: "Updated Name",
            Description: "Updated desc",
            Price: 25.99m,
            Currency: "USD",
            Sku: "INT-UPD-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Updated Category",
            Tags: ["updated"],
            ImageUrl: null,
            IsActive: true,
            UpdatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new ProductUpdatedConsumer(dbContext, Mock.Of<ILogger<ProductUpdatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert
        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(25.99m);
        productPrice.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task ProductUpdatedConsumer_ShouldCreateProductPrice_WhenNotExists()
    {
        // Arrange — product does not exist in cart DB
        var productId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var @event = new ProductUpdatedEvent(
            ProductId: productId,
            Name: "New From Update",
            Description: "Desc",
            Price: 8.50m,
            Currency: "USD",
            Sku: "INT-NEWUPD-001",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Cat",
            Tags: [],
            ImageUrl: null,
            IsActive: true,
            UpdatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new ProductUpdatedConsumer(dbContext, Mock.Of<ILogger<ProductUpdatedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert — should have been created as a fallback
        var productPrice = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId);

        productPrice.Should().NotBeNull();
        productPrice!.Name.Should().Be("New From Update");
        productPrice.Price.Should().Be(8.50m);
        productPrice.Sku.Should().Be("INT-NEWUPD-001");
    }

    // ─── ProductDeletedConsumer ───────────────────────────────────────────

    [Fact]
    public async Task ProductDeletedConsumer_ShouldRemoveProductPrice()
    {
        // Arrange — pre-create the product
        var productId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "INT-DEL-001", "To Delete", 30.00m, "USD"));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

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
    public async Task ProductDeletedConsumer_ShouldNotThrow_WhenProductNotFound()
    {
        // Arrange — product does not exist
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var @event = new ProductDeletedEvent(Guid.NewGuid(), DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert — should not throw
        var consumer = new ProductDeletedConsumer(dbContext, Mock.Of<ILogger<ProductDeletedConsumer>>());
        var act = () => consumer.Consume(consumeContext.Object);
        await act.Should().NotThrowAsync();
    }

    // ─── ProductPriceChangedConsumer ──────────────────────────────────────

    [Fact]
    public async Task ProductPriceChangedConsumer_ShouldUpdatePriceOnly()
    {
        // Arrange — pre-create the product
        var productId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, "INT-PRC-001", "Price Change Product", 50.00m, "USD"));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var @event = new ProductPriceChangedEvent(
            ProductId: productId,
            OldPrice: 50.00m,
            NewPrice: 75.00m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        var consumer = new ProductPriceChangedConsumer(dbContext, Mock.Of<ILogger<ProductPriceChangedConsumer>>());
        await consumer.Consume(consumeContext.Object);

        // Assert — price updated, name unchanged
        var productPrice = await dbContext.ProductPrices.FindAsync(productId);
        productPrice.Should().NotBeNull();
        productPrice!.Price.Should().Be(75.00m);
        productPrice.Currency.Should().Be("USD");
        productPrice.Name.Should().Be("Price Change Product");
    }

    [Fact]
    public async Task ProductPriceChangedConsumer_ShouldNotThrow_WhenProductUnknown()
    {
        // Arrange — product does not exist
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var @event = new ProductPriceChangedEvent(
            ProductId: Guid.NewGuid(),
            OldPrice: 10.00m,
            NewPrice: 15.99m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert — should not throw
        var consumer = new ProductPriceChangedConsumer(dbContext, Mock.Of<ILogger<ProductPriceChangedConsumer>>());
        var act = () => consumer.Consume(consumeContext.Object);
        await act.Should().NotThrowAsync();
    }
}
