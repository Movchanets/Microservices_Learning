namespace Media.API.Application.DTOs;

public sealed record GalleryOrderItem(
    Guid MediaItemId,
    int SortOrder);
