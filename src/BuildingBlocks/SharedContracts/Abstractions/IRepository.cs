namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Generic repository interface. Implemented per-service in Infrastructure layer.
/// Rationale: Enforces that repositories only work with <see cref="AggregateRoot"/> classes,
/// adhering strictly to Domain-Driven Design principles where persistence happens at the aggregate root level.
/// </summary>
/// <typeparam name="T">The type of the aggregate root.</typeparam>
public interface IRepository<T> where T : AggregateRoot
{
    /// <summary>
    /// Retrieves an aggregate root by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate root.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The aggregate root if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a new aggregate root to the repository.
    /// </summary>
    /// <param name="entity">The aggregate root to add.</param>
    void Add(T entity);

    /// <summary>
    /// Updates an existing aggregate root in the repository.
    /// </summary>
    /// <param name="entity">The aggregate root to update.</param>
    void Update(T entity);

    /// <summary>
    /// Removes an aggregate root from the repository.
    /// </summary>
    /// <param name="entity">The aggregate root to remove.</param>
    void Remove(T entity);
}
