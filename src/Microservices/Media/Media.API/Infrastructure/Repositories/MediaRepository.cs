using Media.API.Domain;
using Media.API.Domain.Entities;
using Media.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Media.API.Infrastructure.Repositories;

public sealed class MediaRepository(MediaDbContext context) : IMediaRepository
{
    public async Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.MediaItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<MediaItem>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
        => await context.MediaItems.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);

    public void Add(MediaItem entity) => context.MediaItems.Add(entity);

    public void Remove(MediaItem entity) => context.MediaItems.Remove(entity);
}
