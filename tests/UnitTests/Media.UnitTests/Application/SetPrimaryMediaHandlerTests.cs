using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using MassTransit;
using Media.API.Application.Commands.SetPrimaryMedia;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Media.UnitTests.Application;

public class SetPrimaryMediaHandlerTests
{
    private readonly Mock<IGalleryRepository> _galleryRepo = new();
    private readonly Mock<IMediaRepository> _mediaRepo = new();
    private readonly Mock<IMediaStorageService> _storage = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SetPrimaryMediaHandler>> _logger = new();
    private readonly SetPrimaryMediaHandler _handler;

    public SetPrimaryMediaHandlerTests()
    {
        _handler = new SetPrimaryMediaHandler(
            _galleryRepo.Object,
            _mediaRepo.Object,
            _storage.Object,
            _publishEndpoint.Object,
            _uow.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_SetsPrimary()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var mediaItemId = Guid.NewGuid();
        var entries = new List<GalleryEntry>
        {
            GalleryEntry.Create(mediaItemId, targetId, "Product", 0, false),
            GalleryEntry.Create(Guid.NewGuid(), targetId, "Product", 1, true)
        };
        var mediaItems = new List<MediaItem>
        {
            MediaItem.Create("img.jpg", "image/jpeg", "blob", "https://url", 100, MediaType.Image, null, null)
        };

        _galleryRepo.Setup(r => r.GetByTargetAsync(targetId, "Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mediaRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaItems);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(
            new SetPrimaryMediaCommand(targetId, "Product", mediaItemId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _galleryRepo.Verify(r => r.UpdateRange(It.IsAny<List<GalleryEntry>>()), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<BuildingBlocks.SharedContracts.Events.Media.GalleryUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MediaNotInGallery_ReturnsFailure()
    {
        // Arrange
        _galleryRepo.Setup(r => r.GetByTargetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GalleryEntry>());

        // Act
        var result = await _handler.Handle(
            new SetPrimaryMediaCommand(Guid.NewGuid(), "Product", Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
