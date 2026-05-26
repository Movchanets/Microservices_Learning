namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a new SKU is created on a product.
/// Consumed by Inventory.API to create an InventoryItem (qty=0) for the new SKU.
/// Consumed by Search.API to update the product's SKU list in the search index.
/// Consumed by Cart.API to create a ProductPrice entry for the new SKU.
/// </summary>
public sealed record SkuCreatedIntegrationEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    Guid StoreId,
    decimal Price,
    string Currency,
    Dictionary<string, string> TypedAttributes,
    Dictionary<string, string> FlexibleAttributes,
    DateTime Timestamp);
