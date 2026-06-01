using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Media.API.Application.DTOs;
using Media.API.Application.Queries.GetGallery;
using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Domain.Enums;
using Moq;

namespace Media.UnitTests.Application;

public class GetGalleryHandlerTests
{
    private readonly Mock<IGalleryRepository> _galleryRepo = new();
    private readonly Mock<IMediaRepository> _mediaRepo = new();
    private readonly GetGalleryHandler _handler;

    public GetGalleryHandlerTests()
    {
        _handler = new GetGalleryHandler(_galleryRepo.Object, _mediaRepo.Object);
    }

    [Fact]
    public async Task Handle_WithEntries_ReturnsOrderedDtos()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        var mediaItems = new List<MediaItem>
        {
            MediaItem.Create("primary.jpg", "image/jpeg", "blob1", "https://url1", 100, MediaType.Image, "thumb_blob1", null),
            MediaItem.Create("secondary.jpg", "image/png", "blob2", "https://url2", 200, MediaType.Image, null, null)
        };
        // Assign unique IDs (Guid v7 generates on insert, not in-memory)
        mediaItems[0].Id = Guid.NewGuid();
        mediaItems[1].Id = Guid.NewGuid();
        var mediaId1 = mediaItems[0].Id;
        var mediaId2 = mediaItems[1].Id;

        var entries = new List<GalleryEntry>
        {
            GalleryEntry.Create(mediaId1, targetId, "Product", 0, true),
            GalleryEntry.Create(mediaId2, targetId, "Product", 1, false)
        };

        _galleryRepo.Setup(r => r.GetByTargetAsync(targetId, "Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mediaRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaItems);

        var query = new GetGalleryQuery(targetId, "Product");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].IsPrimary.Should().BeTrue();
        result.Value[0].SortOrder.Should().Be(0);
        result.Value[1].IsPrimary.Should().BeFalse();
        result.Value[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoEntries_ReturnsEmptyList()
    {
        // Arrange
        _galleryRepo.Setup(r => r.GetByTargetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GalleryEntry>());

        var query = new GetGalleryQuery(Guid.NewGuid(), "Product");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MediaItemDeleted_SkipsOrphanedEntry()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        var existingMedia = MediaItem.Create("existing.jpg", "image/jpeg", "blob1", "https://url1", 100, MediaType.Image, null, null);
        existingMedia.Id = Guid.NewGuid();
        var mediaId1 = existingMedia.Id;
        var mediaId2 = Guid.NewGuid(); // Deleted media — not in DB

        var entries = new List<GalleryEntry>
        {
            GalleryEntry.Create(mediaId1, targetId, "Product", 0, true),
            GalleryEntry.Create(mediaId2, targetId, "Product", 1, false)
        };

        // Only one media item returned — mediaId2 was deleted
        var mediaItems = new List<MediaItem> { existingMedia };

        _galleryRepo.Setup(r => r.GetByTargetAsync(targetId, "Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mediaRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaItems);

        var query = new GetGalleryQuery(targetId, "Product");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].FileName.Should().Be("existing.jpg");
    }
}
