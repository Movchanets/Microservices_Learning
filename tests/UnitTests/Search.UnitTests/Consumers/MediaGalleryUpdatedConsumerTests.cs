using BuildingBlocks.SharedContracts.Events.Media;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Search.API.Consumers;
using Search.API.Services;

namespace Search.UnitTests.Consumers;

public class MediaGalleryUpdatedConsumerTests
{
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<ILogger<MediaGalleryUpdatedConsumer>> _logger = new();
    private readonly MediaGalleryUpdatedConsumer _consumer;

    public MediaGalleryUpdatedConsumerTests()
    {
        _consumer = new MediaGalleryUpdatedConsumer(_searchService.Object, _logger.Object);
    }

    [Fact]
    public async Task Consume_ProductWithPrimaryImage_UpdatesImageUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: productId,
            TargetType: "Product",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/primary.jpg", null, 0, true),
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/secondary.jpg", null, 1, false)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchService.Verify(s => s.UpdateProductImageUrlAsync(
            productId, "https://cdn.example.com/primary.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ProductWithNoPrimary_PassesNullUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: productId,
            TargetType: "Product",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/img1.jpg", null, 0, false)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchService.Verify(s => s.UpdateProductImageUrlAsync(
            productId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_SkuTarget_SkipsUpdate()
    {
        // Arrange — SKU targets should be ignored (search only indexes products)
        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: Guid.NewGuid(),
            TargetType: "SKU",
            Items:
            [
                new GalleryItemContract(Guid.NewGuid(), "https://cdn.example.com/sku.jpg", null, 0, true)
            ],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchService.Verify(s => s.UpdateProductImageUrlAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_EmptyGallery_PassesNullUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new GalleryUpdatedIntegrationEvent(
            TargetId: productId,
            TargetType: "Product",
            Items: [],
            Timestamp: DateTime.UtcNow);

        var consumeContext = new Mock<ConsumeContext<GalleryUpdatedIntegrationEvent>>();
        consumeContext.Setup(x => x.Message).Returns(evt);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(consumeContext.Object);

        // Assert
        _searchService.Verify(s => s.UpdateProductImageUrlAsync(
            productId, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
