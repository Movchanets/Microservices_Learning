using Media.API.Domain.Entities;

namespace Media.API.Domain;

public interface IGalleryRepository
{
    Task<List<GalleryEntry>> GetByTargetAsync(Guid targetId, string targetType, CancellationToken ct = default);
    Task<GalleryEntry?> GetByMediaItemAsync(Guid mediaItemId, CancellationToken ct = default);
    void Add(GalleryEntry entity);
    void Remove(GalleryEntry entity);
    void UpdateRange(List<GalleryEntry> entries);
}
