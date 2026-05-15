using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Models;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class ProductUpdatedConsumerTests
{
    private readonly Mock<ISearchService> _searchServiceMock = new();
    private readonly Mock<ILogger<ProductUpdatedConsumer>> _loggerMock = new();
    private readonly ProductUpdatedConsumer _consumer;

    public ProductUpdatedConsumerTests()
    {
        _consumer = new ProductUpdatedConsumer(_searchServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_MapsEventToDocumentAndUpdates()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;

        var @event = new ProductUpdatedEvent(
            productId, "Updated Product", "Updated description",
            49.99m, "EUR", "SKU-002",
            categoryId, "Books",
            new List<string> { "fiction" },
            "http://new-img.jpg", true, updatedAt);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? captured = null;
        _searchServiceMock
            .Setup(x => x.UpdateProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => captured = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        captured.Should().NotBeNull();
        captured!.Id.Should().Be(productId);
        captured.Name.Should().Be("Updated Product");
        captured.Description.Should().Be("Updated description");
        captured.Price.Should().Be(49.99m);
        captured.Currency.Should().Be("EUR");
        captured.Sku.Should().Be("SKU-002");
        captured.CategoryId.Should().Be(categoryId);
        captured.CategoryName.Should().Be("Books");
        captured.Tags.Should().BeEquivalentTo("fiction");
        captured.ImageUrl.Should().Be("http://new-img.jpg");
        captured.IsActive.Should().BeTrue();
        captured.UpdatedAt.Should().Be(updatedAt);

        _searchServiceMock.Verify(
            x => x.UpdateProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenIsActiveFalse_PassesInactiveState()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var @event = new ProductUpdatedEvent(
            productId, "Product", "Desc",
            10m, "USD", "SKU-X",
            Guid.NewGuid(), "Cat",
            new List<string>(), null, false, DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<ProductUpdatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? captured = null;
        _searchServiceMock
            .Setup(x => x.UpdateProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => captured = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        captured!.IsActive.Should().BeFalse();
    }
}
