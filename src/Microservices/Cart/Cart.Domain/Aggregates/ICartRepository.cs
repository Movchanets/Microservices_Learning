using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public interface ICartRepository : IRepository<ShoppingCart>
{
    /// <summary>
    /// Read-only load: cache-first, untracked. Use for queries.
    /// For authenticated users pass buyerId; for anonymous pass cartId.
    /// </summary>
    Task<ShoppingCart> GetCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default);

    /// <summary>
    /// Write path: loads tracked cart from DB (creates if missing). Use for commands.
    /// For authenticated users pass buyerId; for anonymous pass cartId.
    /// </summary>
    Task<ShoppingCart> GetOrCreateTrackedCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default);

    /// <summary>
    /// Persists changes and invalidates cache. Call after domain mutations.
    /// </summary>
    Task SaveCartAsync(ShoppingCart cart, CancellationToken ct = default);

    Task DeleteCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default);
}
