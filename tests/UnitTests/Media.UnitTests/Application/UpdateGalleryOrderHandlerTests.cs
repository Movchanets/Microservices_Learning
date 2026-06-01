using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Media;
using FluentAssertions;
using MassTransit;
using Media.API.Application.Commands.UpdateGalleryOrder;
using Media.API.Application.DTOs;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Media.UnitTests.Application;

public class UpdateGalleryOrderHandlerTests
{
    private readonly Mock<IGalleryRepository> _galleryRepo = new();
    private readonly Mock<IMediaRepository> _mediaRepo = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<UpdateGalleryOrderHandler>> _logger = new();
    private readonly UpdateGalleryOrderHandler _handler;

    public UpdateGalleryOrderHandlerTests()
    {
        _handler = new UpdateGalleryOrderHandler(
            _galleryRepo.Object,
            _mediaRepo.Object,
            _publishEndpoint.Object,
            _uow.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidReorder_UpdatesSortOrderAndPublishes()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var mediaId1 = Guid.NewGuid();
        var mediaId2 = Guid.NewGuid();

        var entries = new List<GalleryEntry>
        {
            GalleryEntry.Create(mediaId1, targetId, "Product", 0, true),
            GalleryEntry.Create(mediaId2, targetId, "Product", 1, false)
        };

        var mediaItems = new List<MediaItem>
        {
            MediaItem.Create("img1.jpg", "image/jpeg", "blob1", "https://url1", 100, MediaType.Image, null, null),
            MediaItem.Create("img2.jpg", "image/jpeg", "blob2", "https://url2", 200, MediaType.Image, null, null)
        };
        mediaItems[0].Id = mediaId1;
        mediaItems[1].Id = mediaId2;

        _galleryRepo.Setup(r => r.GetByTargetAsync(targetId, "Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mediaRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaItems);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var items = new List<GalleryOrderItem>
        {
            new(mediaId1, 1),
            new(mediaId2, 0)
        };
        var command = new UpdateGalleryOrderCommand(targetId, "Product", items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _galleryRepo.Verify(r => r.UpdateRange(It.IsAny<List<GalleryEntry>>()), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<GalleryUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoEntriesFound_ReturnsFailure()
    {
        // Arrange
        _galleryRepo.Setup(r => r.GetByTargetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GalleryEntry>());

        var command = new UpdateGalleryOrderCommand(Guid.NewGuid(), "Product", []);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
