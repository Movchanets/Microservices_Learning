using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class SkuPriceChangedConsumerTests
{
    private readonly Mock<ISearchService> _searchServiceMock = new();
    private readonly Mock<ILogger<SkuPriceChangedConsumer>> _loggerMock = new();
    private readonly SkuPriceChangedConsumer _consumer;

    public SkuPriceChangedConsumerTests()
    {
        _consumer = new SkuPriceChangedConsumer(_searchServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldUpdateProductPriceInSearchIndex()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: skuId,
            SkuCode: "SKU-002",
            OldPrice: 39.99m,
            NewPrice: 49.99m,
            Currency: "EUR",
            ChangedAt: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<SkuPriceChangedEvent>>();
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
        capturedPrice.Should().Be(49.99m);
        capturedCurrency.Should().Be("EUR");

        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 49.99m, "EUR", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WithDifferentPrice_ShouldPassNewPriceToService()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new SkuPriceChangedEvent(
            ProductId: productId,
            SkuId: Guid.NewGuid(),
            SkuCode: "SKU-X",
            OldPrice: 20m,
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
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchServiceMock.Verify(
            x => x.UpdateProductPriceAsync(productId, 10m, "USD", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
