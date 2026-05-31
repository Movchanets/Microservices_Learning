using BuildingBlocks.SharedContracts.Abstractions;
using Media.API.Domain.Enums;

namespace Media.API.Domain.Entities;

/// <summary>
/// Represents a file stored in blob storage (image or video).
/// Created on upload, deleted when the media is removed.
///
/// Relationship: MediaItem is linked to a target (Product/SKU) via GalleryEntry.
/// One MediaItem can be linked to multiple targets (shared media).
/// </summary>
public sealed class MediaItem : Entity
{
    // ── File Metadata ────────────────────────────────────────────

    /// <summary>Original filename as uploaded by the user.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>MIME type (e.g., "image/jpeg", "video/mp4").</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Unique blob name in Azure Blob Storage.</summary>
    public string BlobName { get; private set; } = string.Empty;

    /// <summary>
    /// URL served by Media.API (e.g., "/api/media/{id}").
    /// NOT the raw blob URL — this is the public-facing URL.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; private set; }

    /// <summary>Media type (Image or Video) — derived from ContentType.</summary>
    public MediaType Type { get; private set; }

    // ── Thumbnail ────────────────────────────────────────────────

    /// <summary>
    /// Blob name for auto-generated thumbnail (images only).
    /// Null for videos or if thumbnail generation failed.
    /// </summary>
    public string? ThumbnailBlobName { get; private set; }

    // ── Audit ────────────────────────────────────────────────────

    public DateTime CreatedAt { get; private init; }

    /// <summary>User ID who uploaded this media (null for system uploads).</summary>
    public string? CreatedBy { get; private set; }

    // ── Constructor ──────────────────────────────────────────────

    // EF Core constructor
    private MediaItem() { }

    // ── Factory ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a new MediaItem. Validates that all required fields are present.
    /// The URL is initially set to the blob storage URL — call <see cref="SetUrl"/>
    /// to override with the API-served URL (e.g., "/api/media/{id}").
    /// </summary>
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

    // ── Behavior ─────────────────────────────────────────────────

    /// <summary>
    /// Updates the public URL. Called after creation to set the API-served URL
    /// (e.g., "/api/media/{id}") instead of the raw blob storage URL.
    /// </summary>
    public void SetUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Url = url.Trim();
    }
}
