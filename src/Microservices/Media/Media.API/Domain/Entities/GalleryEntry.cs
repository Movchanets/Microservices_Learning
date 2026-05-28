using BuildingBlocks.SharedContracts.Abstractions;

namespace Media.API.Domain.Entities;

public sealed class GalleryEntry : Entity
{
    public Guid MediaItemId { get; private set; }
    public Guid TargetId { get; private set; }
    public string TargetType { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private init; }

    // EF Core constructor
    private GalleryEntry() { }

    public static GalleryEntry Create(
        Guid mediaItemId,
        Guid targetId,
        string targetType,
        int sortOrder,
        bool isPrimary)
    {
        if (mediaItemId == Guid.Empty)
            throw new ArgumentException("MediaItemId is required", nameof(mediaItemId));
        if (targetId == Guid.Empty)
            throw new ArgumentException("TargetId is required", nameof(targetId));
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);

        return new GalleryEntry
        {
            MediaItemId = mediaItemId,
            TargetId = targetId,
            TargetType = targetType.Trim().ToUpperInvariant(), // normalize to prevent case-sensitivity bugs
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
