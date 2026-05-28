namespace BuildingBlocks.SharedContracts.Events.Media;

/// <summary>
/// Published by Media.API when a media item is deleted.
/// Consumed by Catalog.API to clear cached image URLs.
/// </summary>
public sealed record MediaDeletedIntegrationEvent(
    Guid MediaItemId,
    Guid TargetId,
    string TargetType,
    bool WasPrimary,
    DateTime Timestamp);
