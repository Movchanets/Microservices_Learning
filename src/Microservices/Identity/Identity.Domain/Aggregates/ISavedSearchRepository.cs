namespace Identity.Domain.Aggregates;

public interface ISavedSearchRepository
{
    void Add(SavedSearch search);
    Task<SavedSearch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SavedSearch>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
