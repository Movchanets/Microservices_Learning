namespace BuildingBlocks.SharedContracts.Events.Media;

public sealed record GalleryItemContract(
    Guid MediaItemId,
    string Url,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary);
