using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Models;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class ProductCreatedConsumerTests
{
    private readonly Mock<ISearchService> _searchServiceMock = new();
    private readonly Mock<ILogger<ProductCreatedConsumer>> _loggerMock = new();
    private readonly ProductCreatedConsumer _consumer;

    public ProductCreatedConsumerTests()
    {
        _consumer = new ProductCreatedConsumer(_searchServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_MapsEventToDocumentAndIndexes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var @event = new ProductCreatedEvent(
            productId, "Test Product", "A test product",
            29.99m, "USD", "SKU-001",
            categoryId, "Electronics",
            new List<string> { "gadget", "tech" },
            "http://img.jpg", storeId, createdAt);

        var consumeContext = new Mock<ConsumeContext<ProductCreatedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        ProductSearchDocument? captured = null;
        _searchServiceMock
            .Setup(x => x.IndexProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSearchDocument, CancellationToken>((doc, _) => captured = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        captured.Should().NotBeNull();
        captured!.Id.Should().Be(productId);
        captured.Name.Should().Be("Test Product");
        captured.Description.Should().Be("A test product");
        captured.Price.Should().Be(29.99m);
        captured.Currency.Should().Be("USD");
        captured.Sku.Should().Be("SKU-001");
        captured.CategoryId.Should().Be(categoryId);
        captured.CategoryName.Should().Be("Electronics");
        captured.Tags.Should().BeEquivalentTo("gadget", "tech");
        captured.ImageUrl.Should().Be("http://img.jpg");
        captured.StoreId.Should().Be(storeId);
        captured.IsActive.Should().BeTrue();
        captured.CreatedAt.Should().Be(createdAt);
        captured.UpdatedAt.Should().Be(createdAt);

        _searchServiceMock.Verify(
            x => x.IndexProductAsync(It.IsAny<ProductSearchDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
