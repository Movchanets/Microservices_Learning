using BuildingBlocks.SharedContracts.Abstractions;
using Media.API.Domain.Enums;

namespace Media.API.Domain.Entities;

public sealed class MediaItem : Entity
{
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public MediaType Type { get; private set; }
    public string? ThumbnailBlobName { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public string? CreatedBy { get; private set; }

    // EF Core constructor
    private MediaItem() { }

    public static MediaItem Create(
        string fileName,
        string contentType,
        string blobName,
        string url,
        long sizeBytes,
        MediaType type,
        string? thumbnailBlobName,
        string? createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new MediaItem
        {
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            BlobName = blobName.Trim(),
            Url = url.Trim(),
            SizeBytes = sizeBytes,
            Type = type,
            ThumbnailBlobName = thumbnailBlobName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void SetUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Url = url.Trim();
    }
}
