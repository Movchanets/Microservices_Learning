using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Catalog product lifecycle events
/// are correctly consumed by the Inventory microservice.
///
/// These tests use pure Moq mocks for the repository and unit of work,
/// verifying the consumer's interaction contract without any database.
/// </summary>
public class CatalogToInventoryContractTests
{
    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ILogger<Inventory.Infrastructure.Messaging.Consumers.ProductCreatedConsumer> _logger;

    public CatalogToInventoryContractTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _logger = Mock.Of<ILogger<Inventory.Infrastructure.Messaging.Consumers.ProductCreatedConsumer>>();
    }

    [Fact]
    public async Task ProductCreatedEvent_Contract_ShouldCreateInventoryItemWithZeroStock()
    {
        // Arrange
        var sku = $"SKU-INV-{Guid.NewGuid():N}";
        var @event = new ProductCreatedEvent(
            ProductId: Guid.NewGuid(),
            Name: "Inventory Test Product",
            Description: "Desc",
            Price: 49.99m,
            Currency: "USD",
            Sku: sku,
            CategoryId: Guid.NewGuid(),
            CategoryName: "Test",
            Tags: [],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // SKU does not exist yet
        _repositoryMock
            .Setup(r => r.GetBySkuAsync(sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var consumer = new Inventory.Infrastructure.Messaging.Consumers.ProductCreatedConsumer(
            _repositoryMock.Object, _uowMock.Object, _logger);
        await consumer.Consume(consumeContext.Object);

        // Assert
        _repositoryMock.Verify(
            r => r.Add(It.Is<InventoryItem>(i =>
                i.AvailableQuantity == 0)),
            Times.Once,
            "repository.Add should be called with an InventoryItem that has zero stock");
        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "SaveChangesAsync should be called to persist the new item");
    }

    [Fact]
    public async Task ProductCreatedEvent_Contract_ShouldBeIdempotentForDuplicateSku()
    {
        // Arrange
        var sku = $"SKU-IDEM-{Guid.NewGuid():N}";
        var productId = Guid.NewGuid();
        var existingItem = InventoryItem.Create(sku, 50, Guid.Parse("33333333-3333-3333-3333-333333333333"), productId);

        var @event = new ProductCreatedEvent(
            ProductId: productId,
            Name: "Duplicate Product",
            Description: "Desc",
            Price: 10m,
            Currency: "USD",
            Sku: sku,
            CategoryId: Guid.NewGuid(),
            CategoryName: "Cat",
            Tags: [],
            ImageUrl: null,
            StoreId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // SKU already exists with the same ProductId
        _repositoryMock
            .Setup(r => r.GetBySkuAsync(sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItem);

        // Act
        var consumer = new Inventory.Infrastructure.Messaging.Consumers.ProductCreatedConsumer(
            _repositoryMock.Object, _uowMock.Object, _logger);
        await consumer.Consume(consumeContext.Object);

        // Assert - repository.Add should NOT be called (idempotency)
        _repositoryMock.Verify(
            r => r.Add(It.IsAny<InventoryItem>()),
            Times.Never,
            "repository.Add should not be called when SKU already exists");
        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "SaveChangesAsync should not be called when SKU and ProductId already match");
    }
}
