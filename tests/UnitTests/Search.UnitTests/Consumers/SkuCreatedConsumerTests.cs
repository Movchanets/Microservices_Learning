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
    public async Task Consume_ShouldUpdateProductPriceInSearchIndex()
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

        Guid? capturedProductId = null;
        decimal? capturedPrice = null;
        string? capturedCurrency = null;
        _searchServiceMock
            .Setup(x => x.UpdateProductPriceAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, decimal, string, CancellationToken>((id, price, currency, _) =>
            {
                capturedProductId = id;
                capturedPrice = price;
                capturedCurrency = currency;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        capturedProductId.Should().Be(productId);
        capturedPrice.Should().Be(29.99m);
        capturedCurrency.Should().Be("USD");

        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 29.99m, "USD", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
