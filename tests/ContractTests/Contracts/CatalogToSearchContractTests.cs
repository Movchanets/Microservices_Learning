using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Services;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Catalog product lifecycle events
/// are correctly consumed by the Search microservice (Elasticsearch).
///
/// Ensures the message contract between Catalog publisher and
/// Search consumer is stable and compatible.
/// </summary>
public class CatalogToSearchContractTests
{
    private readonly Mock<ISearchService> _searchServiceMock;
    private readonly SkuCreatedConsumer _createdConsumer;
    private readonly SkuPriceChangedConsumer _updatedConsumer;
    private readonly ProductDeletedConsumer _deletedConsumer;

    public CatalogToSearchContractTests()
    {
        _searchServiceMock = new Mock<ISearchService>();
        _createdConsumer = new SkuCreatedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<SkuCreatedConsumer>>());
        _updatedConsumer = new SkuPriceChangedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<SkuPriceChangedConsumer>>());
        _deletedConsumer = new ProductDeletedConsumer(
            _searchServiceMock.Object, Mock.Of<ILogger<ProductDeletedConsumer>>());
    }

    [Fact]
    public async Task SkuCreatedIntegrationEvent_Contract_ShouldUpdatePriceInSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-SEARCH-001",
            ProductName: "Searchable Product",
            StoreId: storeId,
            Price: 99.99m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _searchServiceMock
            .Setup(x => x.UpdateProductPriceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _createdConsumer.Consume(consumeContext.Object);

        // Assert - verify the contract mapping
        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 99.99m, "USD", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldUpdatePriceInSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: Guid.NewGuid(),
            SkuCode: "SKU-SEARCH-UPD",
            OldPrice: 99.99m,
            NewPrice: 149.99m,
            Currency: "EUR",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _searchServiceMock
            .Setup(x => x.UpdateProductPriceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updatedConsumer.Consume(consumeContext.Object);

        // Assert
        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 149.99m, "EUR", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SkuPriceChangedEvent_Contract_ShouldPassNewPriceToService()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: Guid.NewGuid(),
            SkuCode: "SKU-INACTIVE",
            OldPrice: 50m,
            NewPrice: 10m,
            Currency: "USD",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _searchServiceMock
            .Setup(x => x.UpdateProductPriceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updatedConsumer.Consume(consumeContext.Object);

        // Assert
        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 10m, "USD", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProductDeletedEvent_Contract_ShouldRemoveProductFromSearch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductDeletedEvent(productId, DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        Guid? capturedId = null;
        _searchServiceMock
            .Setup(x => x.DeleteProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => capturedId = id)
            .Returns(Task.CompletedTask);

        // Act
        await _deletedConsumer.Consume(consumeContext.Object);

        // Assert
        capturedId.Should().Be(productId);
    }
}
