using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Aggregates;

public interface IInventoryItemRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<List<InventoryItem>> GetBySkusAsync(IEnumerable<string> skus, CancellationToken ct = default);
    Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default);
}