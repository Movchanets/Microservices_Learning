using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class ProductDeletedConsumerTests
{
    private readonly Mock<ISearchService> _searchServiceMock = new();
    private readonly Mock<ILogger<ProductDeletedConsumer>> _loggerMock = new();
    private readonly ProductDeletedConsumer _consumer;

    public ProductDeletedConsumerTests()
    {
        _consumer = new ProductDeletedConsumer(_searchServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_CallsDeleteWithCorrectProductId()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductDeletedEvent(productId, DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductDeletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        Guid? deletedId = null;
        _searchServiceMock
            .Setup(x => x.DeleteProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => deletedId = id)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        deletedId.Should().Be(productId);
        _searchServiceMock.Verify(
            x => x.DeleteProductAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
