namespace BuildingBlocks.SharedContracts.Events.Media;

/// <summary>
/// Represents a single gallery item within an integration event.
/// Used by GalleryUpdatedIntegrationEvent to carry the full gallery state.
///
/// URLs are API-served (e.g., "/api/media/{id}") — NOT raw blob storage URLs.
/// The BFF resolves these to absolute URLs for the frontend.
/// </summary>
public sealed record GalleryItemContract(
    Guid MediaItemId,
    string Url,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary);
