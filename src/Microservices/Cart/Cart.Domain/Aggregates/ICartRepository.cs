using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public interface ICartRepository : IRepository<ShoppingCart>
{
    Task<ShoppingCart> GetCartAsync(string buyerId, CancellationToken ct = default);
    Task<ShoppingCart> UpdateCartAsync(ShoppingCart cart, CancellationToken ct = default);
    Task DeleteCartAsync(string buyerId, CancellationToken ct = default);
}