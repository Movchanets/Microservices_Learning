using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Aggregates;

public interface IOrderRepository : IRepository<Order>
{
    Task<List<Order>> GetByBuyerIdAsync(string buyerId, CancellationToken ct = default);
    Task<List<Order>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
}
