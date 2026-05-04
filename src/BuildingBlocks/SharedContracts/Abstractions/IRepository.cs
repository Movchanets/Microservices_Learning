namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Generic repository interface. Implemented per-service in Infrastructure layer.
/// </summary>
public interface IRepository<T> where T : AggregateRoot
{
	Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
	void Add(T entity);
	void Update(T entity);
	void Remove(T entity);
}
