using Media.API.Domain.Entities;

namespace Media.API.Application;

/// <summary>
/// Shared URL building for media items. All handlers MUST use these instead of
/// calling storageService.GetUrl() — blob URLs are internal infrastructure and
/// must never leak into integration events or DTOs.
///
/// URL pattern:
///   File:      /api/media/{mediaId}
///   Thumbnail: /api/media/{mediaId}/thumbnail
///
/// These are relative URLs — the BFF resolves them to absolute URLs
/// using the gateway origin (e.g., http://localhost:5293).
/// </summary>
public static class MediaUrlExtensions
{
    /// <summary>
    /// Returns the API-served URL for this media item (e.g., "/api/media/{id}").
    /// This is the public-facing URL — NOT the internal blob storage URL.
    /// </summary>
    public static string GetMediaUrl(this MediaItem media)
        => $"/api/media/{media.Id}";

    /// <summary>
    /// Returns the API-served URL for this media item's thumbnail, or null
    /// if no thumbnail exists (e.g., videos or failed thumbnail generation).
    /// </summary>
    public static string? GetThumbnailUrl(this MediaItem media)
        => media.ThumbnailBlobName is not null
            ? $"/api/media/{media.Id}/thumbnail"
            : null;
}
