using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public interface ICartRepository : IRepository<ShoppingCart>
{
    /// <summary>
    /// Read-only load: cache-first, untracked. Use for queries.
    /// </summary>
    Task<ShoppingCart> GetCartAsync(string buyerId, CancellationToken ct = default);

    /// <summary>
    /// Write path: loads tracked cart from DB (creates if missing). Use for commands.
    /// </summary>
    Task<ShoppingCart> GetOrCreateTrackedCartAsync(string buyerId, CancellationToken ct = default);

    /// <summary>
    /// Persists changes and invalidates cache. Call after domain mutations.
    /// </summary>
    Task SaveCartAsync(ShoppingCart cart, CancellationToken ct = default);

    Task DeleteCartAsync(string buyerId, CancellationToken ct = default);
}
