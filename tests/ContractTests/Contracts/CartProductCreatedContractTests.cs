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
/// Contract tests verifying Cart consumer behavior for the SKU refactor.
///
/// After the SKU refactor, Cart consumers handle:
///   - SkuCreatedIntegrationEvent → upserts ProductPrice via SkuCreatedConsumer
///   - SkuPriceChangedEvent → updates price via SkuPriceChangedConsumer
///   - ProductDeletedEvent → removes all ProductPrice entries via ProductDeletedConsumer
/// </summary>
public class CartProductCreatedContractTests
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
    public async Task SkuCreatedEvent_ValidSku_ShouldCreateProductPrice()
    {
        // Arrange — event with valid SKU
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "ELEC-IPHONE-16-PRO",
            ProductName: "Valid SKU Product",
            StoreId: storeId,
            Price: 49.99m,
            Currency: "EUR",
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

        // Assert — ProductPrice should be created with correct data
        var productPrice = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.SkuId == skuId);
        productPrice.Should().NotBeNull();
        productPrice!.SkuCode.Should().Be("ELEC-IPHONE-16-PRO");
        productPrice.Name.Should().Be("Valid SKU Product");
        productPrice.Price.Should().Be(49.99m);
        productPrice.Currency.Should().Be("EUR");
        productPrice.StoreId.Should().Be(storeId);
        productPrice.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task SkuCreatedEvent_DuplicateEvent_ShouldUpsert()
    {
        // Arrange — same event sent twice (message broker replay)
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "REPLAY-SKU-001",
            ProductName: "Replay Product",
            StoreId: Guid.NewGuid(),
            Price: 25.00m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act — consume twice
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var consumer1 = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer1.Consume(consumeContext.Object);

        var consumer2 = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
        await consumer2.Consume(consumeContext.Object);

        // Assert — only one record, idempotent
        var count = await dbContext.ProductPrices.CountAsync(p => p.SkuId == skuId);
        count.Should().Be(1, "duplicate events should be idempotent");
    }

    [Fact]
    public async Task SkuCreatedEvent_MultipleProducts_ShouldCreateSeparateRecords()
    {
        // Arrange — multiple different SKUs
        var events = Enumerable.Range(0, 5).Select(i =>
        {
            var ctx = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
            ctx.Setup(x => x.Message).Returns(new SkuCreatedIntegrationEvent(
                ProductId: Guid.NewGuid(),
                SkuId: Guid.NewGuid(),
                SkuCode: $"SKU-{i:D3}",
                ProductName: $"Product {i}",
                StoreId: Guid.NewGuid(),
                Price: i * 10m,
                Currency: "USD",
                TypedAttributes: new Dictionary<string, string>(),
                FlexibleAttributes: new Dictionary<string, string>(),
                Timestamp: DateTime.UtcNow));
            ctx.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
            return ctx;
        }).ToList();

        // Act — consume all events
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        foreach (var ctx in events)
        {
            var consumer = new SkuCreatedConsumer(repository, Mock.Of<ILogger<SkuCreatedConsumer>>());
            await consumer.Consume(ctx.Object);
        }

        // Assert — five records created
        var count = await dbContext.ProductPrices.CountAsync();
        count.Should().Be(5, "each unique SKU should create its own ProductPrice record");
    }
}
