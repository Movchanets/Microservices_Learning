namespace BuildingBlocks.SharedContracts.Events.Media;

/// <summary>
/// Published by Media.API when a new media item is uploaded and added to a gallery.
/// Consumed by Catalog.API to update product/SKU image URLs.
/// </summary>
public sealed record MediaUploadedIntegrationEvent(
    Guid MediaItemId,
    Guid TargetId,
    string TargetType,
    string Url,
    string? ThumbnailUrl,
    bool IsPrimary,
    DateTime Timestamp,
    Guid? LinkedProductId = null);
