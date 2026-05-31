using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Media.API.Infrastructure.Repositories;

/// <summary>
/// Repository for GalleryEntry entities. Links media items to targets (Product/SKU).
///
/// Key convention: TargetType is stored UPPERCASE in the database (see GalleryEntry.Create).
/// All queries normalize targetType with ToUpperInvariant() to prevent case-sensitivity bugs.
/// </summary>
public sealed class GalleryRepository(MediaDbContext context) : IGalleryRepository
{
    /// <summary>
    /// Returns all gallery entries for a target, ordered by SortOrder.
    /// TargetType is normalized to UPPERCASE for consistent matching.
    /// </summary>
    public async Task<List<GalleryEntry>> GetByTargetAsync(
        Guid targetId, string targetType, CancellationToken ct = default)
        => await context.GalleryEntries
            .Where(x => x.TargetId == targetId
                     && x.TargetType == targetType.ToUpperInvariant())
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

    /// <summary>
    /// Returns the gallery entry for a specific media item.
    /// Used during deletion to find the associated target.
    /// </summary>
    public async Task<GalleryEntry?> GetByMediaItemAsync(
        Guid mediaItemId, CancellationToken ct = default)
        => await context.GalleryEntries
            .FirstOrDefaultAsync(x => x.MediaItemId == mediaItemId, ct);

    public void Add(GalleryEntry entity) => context.GalleryEntries.Add(entity);

    public void Remove(GalleryEntry entity) => context.GalleryEntries.Remove(entity);

    public void UpdateRange(List<GalleryEntry> entries) => context.GalleryEntries.UpdateRange(entries);
}
