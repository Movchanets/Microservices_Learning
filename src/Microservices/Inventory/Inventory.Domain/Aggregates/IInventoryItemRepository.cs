using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Aggregates;

public interface IInventoryItemRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetBySkuIdAsync(Guid skuId, CancellationToken ct = default);
    Task<InventoryItem?> GetBySkuCodeAsync(string skuCode, CancellationToken ct = default);
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<InventoryItem?> GetByStoreAndSkuCodeAsync(Guid storeId, string skuCode, CancellationToken ct = default);
    Task<List<InventoryItem>> GetBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default);
    Task<List<InventoryItem>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default);
}
