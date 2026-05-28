using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class SkuCreatedConsumerTests
{
    private readonly Mock<ISearchService> _searchServiceMock = new();
    private readonly Mock<ILogger<SkuCreatedConsumer>> _loggerMock = new();
    private readonly SkuCreatedConsumer _consumer;

    public SkuCreatedConsumerTests()
    {
        _consumer = new SkuCreatedConsumer(_searchServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldAddSkuToProductInSearchIndex()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var @event = new SkuCreatedIntegrationEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-001",
            ProductName: "Test Product",
            StoreId: storeId,
            Price: 29.99m,
            Currency: "USD",
            TypedAttributes: new Dictionary<string, string>(),
            FlexibleAttributes: new Dictionary<string, string>(),
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuCreatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        _searchServiceMock
            .Setup(x => x.AddSkuToProductAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchServiceMock.Verify(
            x => x.AddSkuToProductAsync(productId, 29.99m, "USD", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
