using BuildingBlocks.SharedContracts.Dtos;
using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

/// <summary>
/// Shared helper for resolving inventory items from a list of order items.
/// Resolves by SkuId (preferred) with fallback to ProductId for legacy items.
/// </summary>
internal static class InventoryItemResolver
{
    public static async Task<Dictionary<OrderItemContract, InventoryItem?>> ResolveAsync(
        List<OrderItemContract> items,
        IInventoryItemRepository repository,
        CancellationToken ct)
    {
        var skuIds = items
            .Where(i => i.SkuId != Guid.Empty)
            .Select(i => i.SkuId)
            .ToList();

        var bySkuId = skuIds.Count > 0
            ? (await repository.GetBySkuIdsAsync(skuIds, ct)).ToDictionary(i => i.SkuId)
            : [];

        var result = new Dictionary<OrderItemContract, InventoryItem?>();
        foreach (var item in items)
        {
            InventoryItem? inventoryItem = null;

            if (item.SkuId != Guid.Empty)
                bySkuId.TryGetValue(item.SkuId, out inventoryItem);
            else
                inventoryItem = await repository.GetByProductIdAsync(item.ProductId, ct);

            result[item] = inventoryItem;
        }

        return result;
    }
}
