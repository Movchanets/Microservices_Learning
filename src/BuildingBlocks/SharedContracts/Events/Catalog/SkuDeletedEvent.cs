namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a SKU is deleted from a product.
/// Consumed by Inventory.API to deactivate the corresponding InventoryItem.
/// Consumed by Search.API to remove the SKU from the search index.
/// </summary>
public sealed record SkuDeletedEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode,
    DateTime DeletedAt);
