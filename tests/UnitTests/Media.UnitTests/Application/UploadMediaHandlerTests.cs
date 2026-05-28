using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using MassTransit;
using BuildingBlocks.SharedContracts.Events.Media;
using Media.API.Application.Commands.UploadMedia;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Media.UnitTests.Application;

public class UploadMediaHandlerTests
{
    private readonly Mock<IMediaRepository> _mediaRepo = new();
    private readonly Mock<IGalleryRepository> _galleryRepo = new();
    private readonly Mock<IMediaStorageService> _storage = new();
    private readonly ImageProcessingService _imageProcessing = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<UploadMediaHandler>> _logger = new();
    private readonly UploadMediaHandler _handler;

    public UploadMediaHandlerTests()
    {
        _handler = new UploadMediaHandler(
            _mediaRepo.Object,
            _galleryRepo.Object,
            _storage.Object,
            _imageProcessing,
            _publishEndpoint.Object,
            _uow.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidImage_UploadsAndReturnsDto()
    {
        // Arrange
        var stream = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(
            stream, "test.jpg", "image/jpeg",
            Guid.NewGuid(), "Product", true);

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaStorageResult("blob.jpg", "https://storage/blob.jpg", 100));

        _storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaStorageResult("thumb_blob.jpg", "https://storage/thumb_blob.jpg", 50));

        _galleryRepo.Setup(r => r.GetByTargetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GalleryEntry>());

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().Be("test.jpg");
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.IsPrimary.Should().BeTrue();

        _mediaRepo.Verify(r => r.Add(It.IsAny<MediaItem>()), Times.Once);
        _galleryRepo.Verify(r => r.Add(It.IsAny<GalleryEntry>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<MediaUploadedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidContentType_ReturnsFailure()
    {
        // Arrange
        var stream = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(
            stream, "test.txt", "text/plain",
            Guid.NewGuid(), "Product", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CONTENT_TYPE");
    }

    [Fact]
    public async Task Handle_FileTooLarge_ReturnsFailure()
    {
        // Arrange — 11MB image exceeds 10MB limit
        var stream = new MemoryStream(new byte[11 * 1024 * 1024]);
        var command = new UploadMediaCommand(
            stream, "huge.jpg", "image/jpeg",
            Guid.NewGuid(), "Product", false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_TOO_LARGE");
    }
}
