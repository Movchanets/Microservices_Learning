namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Unit of Work interface. Implemented per-service wrapping DbContext.SaveChanges().
/// Rationale: Abstracts the transaction/persistence mechanism (like EF Core's DbContext)
/// from the application layer, allowing changes across multiple aggregates to be committed atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes to the underlying data store asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of state entries written to the underlying data store.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
