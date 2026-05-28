using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Media.API.Infrastructure.Repositories;

public sealed class GalleryRepository(MediaDbContext context) : IGalleryRepository
{
    public async Task<List<GalleryEntry>> GetByTargetAsync(
        Guid targetId, string targetType, CancellationToken ct = default)
        => await context.GalleryEntries
            .Where(x => x.TargetId == targetId && x.TargetType == targetType.ToUpperInvariant())
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

    public async Task<GalleryEntry?> GetByMediaItemAsync(
        Guid mediaItemId, CancellationToken ct = default)
        => await context.GalleryEntries
            .FirstOrDefaultAsync(x => x.MediaItemId == mediaItemId, ct);

    public void Add(GalleryEntry entity) => context.GalleryEntries.Add(entity);

    public void Remove(GalleryEntry entity) => context.GalleryEntries.Remove(entity);

    public void UpdateRange(List<GalleryEntry> entries) => context.GalleryEntries.UpdateRange(entries);
}
