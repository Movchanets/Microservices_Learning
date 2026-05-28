using Media.API.Domain.Entities;

namespace Media.API.Domain;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<MediaItem>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
    void Add(MediaItem entity);
    void Remove(MediaItem entity);
}
