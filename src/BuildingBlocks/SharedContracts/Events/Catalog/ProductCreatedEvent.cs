namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a new product is created.
/// Consumed by Search.API to index the product in Elasticsearch.
/// Consumed by Inventory.API to create inventory records (legacy path).
/// Price/Sku data should be consumed from SkuCreatedIntegrationEvent instead.
/// </summary>
public sealed record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
