using BuildingBlocks.SharedContracts.Abstractions;

namespace Media.API.Domain.Entities;

/// <summary>
/// Links a MediaItem to a target entity (Product or SKU).
/// This is the join table in the normalized media model.
///
/// Design:
///   MediaItem → file in blob storage (one per upload)
///   GalleryEntry → links media to a target (many per target)
///   One MediaItem can be linked to multiple targets (shared media).
///
/// TargetType is normalized to UPPERCASE on creation to prevent
/// case-sensitivity bugs (e.g., "Sku" vs "SKU" vs "sku").
/// </summary>
public sealed class GalleryEntry : Entity
{
    // ── Relationships ────────────────────────────────────────────

    /// <summary>The media file this entry points to.</summary>
    public Guid MediaItemId { get; private set; }

    /// <summary>The target entity (Product or SKU) this media belongs to.</summary>
    public Guid TargetId { get; private set; }

    /// <summary>
    /// Type of target entity (e.g., "PRODUCT", "SKU").
    /// Normalized to UPPERCASE on creation — always compare with OrdinalIgnoreCase.
    /// </summary>
    public string TargetType { get; private set; } = string.Empty;

    // ── Gallery Ordering ─────────────────────────────────────────

    /// <summary>Display order within the gallery (0-based). Lower = first.</summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this media is the primary (thumbnail) image for the target.
    /// Only one entry per target should be primary at a time.
    /// </summary>
    public bool IsPrimary { get; private set; }

    // ── Audit ────────────────────────────────────────────────────

    public DateTime CreatedAt { get; private init; }

    // ── Constructor ──────────────────────────────────────────────

    // EF Core constructor
    private GalleryEntry() { }

    // ── Factory ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a new GalleryEntry linking a media item to a target.
    /// TargetType is normalized to UPPERCASE to prevent case-sensitivity bugs.
    /// </summary>
    /// <param name="mediaItemId">The media file to link.</param>
    /// <param name="targetId">The target entity (Product/SKU).</param>
    /// <param name="targetType">Target type ("Product", "SKU") — normalized to UPPERCASE.</param>
    /// <param name="sortOrder">Display order (0-based).</param>
    /// <param name="isPrimary">Whether this is the primary/thumbnail image.</param>
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
            TargetType = targetType.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Behavior ─────────────────────────────────────────────────

    /// <summary>Updates the display order within the gallery.</summary>
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    /// <summary>Sets or unsets this entry as the primary image for its target.</summary>
    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
