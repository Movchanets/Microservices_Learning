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

    public async Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.Sku == sku, ct);
    }

    public async Task<List<InventoryItem>> GetBySkusAsync(IEnumerable<string> skus, CancellationToken ct = default)
    {
        var skuList = skus.ToList();
        return await dbContext.InventoryItems
            .Where(i => skuList.Contains(i.Sku))
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