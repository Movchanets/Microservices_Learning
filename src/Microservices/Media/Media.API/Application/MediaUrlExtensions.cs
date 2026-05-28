using Media.API.Domain.Entities;

namespace Media.API.Application;

/// <summary>
/// Shared URL building for media items. All handlers MUST use these instead of
/// calling storageService.GetUrl() — blob URLs are internal infrastructure and
/// must never leak into integration events or DTOs.
/// </summary>
public static class MediaUrlExtensions
{
    public static string GetMediaUrl(this MediaItem media)
        => $"/api/media/{media.Id}";

    public static string? GetThumbnailUrl(this MediaItem media)
        => media.ThumbnailBlobName is not null ? $"/api/media/{media.Id}/thumbnail" : null;
}
