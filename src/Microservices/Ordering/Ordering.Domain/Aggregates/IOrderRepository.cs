using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Aggregates;

public interface IOrderRepository : IRepository<Order>
{
    Task<List<Order>> GetByBuyerIdAsync(string buyerId, CancellationToken ct = default);
    Task<List<Order>> GetBySellerIdAsync(string sellerId, CancellationToken ct = default);
}
