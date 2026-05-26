using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class InventoryItemRepository(InventoryDbContext dbContext) : IInventoryItemRepository
{
    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FindAsync([id], cancellationToken: ct);
    }

    public async Task<InventoryItem?> GetBySkuIdAsync(Guid skuId, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.SkuId == skuId, ct);
    }

    public async Task<InventoryItem?> GetBySkuCodeAsync(string skuCode, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.SkuCode == skuCode, ct);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
    }

    public async Task<InventoryItem?> GetByStoreAndSkuCodeAsync(Guid storeId, string skuCode, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FirstOrDefaultAsync(
            i => i.StoreId == storeId && i.SkuCode == skuCode, ct);
    }

    public async Task<List<InventoryItem>> GetBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default)
    {
        var idList = skuIds.ToList();
        return await dbContext.InventoryItems
            .Where(i => idList.Contains(i.SkuId))
            .ToListAsync(ct);
    }

    public async Task<List<InventoryItem>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems
            .Where(i => i.StoreId == storeId)
            .ToListAsync(ct);
    }

    public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.ToListAsync(ct);
    }

    public void Add(InventoryItem item)
    {
        dbContext.InventoryItems.Add(item);
    }

    public void Update(InventoryItem item)
    {
        dbContext.InventoryItems.Update(item);
    }

    public void Remove(InventoryItem item)
    {
        dbContext.InventoryItems.Remove(item);
    }
}
