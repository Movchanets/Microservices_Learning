using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using MassTransit;
using Media.API.Application.Commands.DeleteMedia;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Media.UnitTests.Application;

public class DeleteMediaHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepo = new();
    private readonly Mock<IGalleryRepository> _galleryRepo = new();
    private readonly Mock<IMediaStorageService> _storage = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<DeleteMediaHandler>> _logger = new();
    private readonly DeleteMediaHandler _handler;

    public DeleteMediaHandlerTests()
    {
        _handler = new DeleteMediaHandler(
            _mediaRepo.Object,
            _galleryRepo.Object,
            _storage.Object,
            _publishEndpoint.Object,
            _uow.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ExistingMedia_DeletesAndReturnsSuccess()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var media = MediaItem.Create("test.jpg", "image/jpeg", "blob.jpg", "https://url", 100, MediaType.Image, null, null);
        var galleryEntry = GalleryEntry.Create(mediaId, Guid.NewGuid(), "Product", 0, true);

        _mediaRepo.Setup(r => r.GetByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        _galleryRepo.Setup(r => r.GetByMediaItemAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(galleryEntry);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(new DeleteMediaCommand(mediaId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mediaRepo.Verify(r => r.Remove(It.IsAny<MediaItem>()), Times.Once);
        _galleryRepo.Verify(r => r.Remove(It.IsAny<GalleryEntry>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MediaNotFound_ReturnsFailure()
    {
        // Arrange
        _mediaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        // Act
        var result = await _handler.Handle(new DeleteMediaCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
