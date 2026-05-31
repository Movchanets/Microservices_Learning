namespace BuildingBlocks.SharedContracts.Events.Media;

/// <summary>
/// Published by Media.API when gallery order or primary image changes.
/// Consumed by Catalog.API, Cart.API to update cached product images.
/// </summary>
public sealed record GalleryUpdatedIntegrationEvent(
    Guid TargetId,
    string TargetType,
    List<GalleryItemContract> Items,
    DateTime Timestamp);
