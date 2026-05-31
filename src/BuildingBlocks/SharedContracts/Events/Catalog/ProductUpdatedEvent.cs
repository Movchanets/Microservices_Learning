namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when product details are modified.
/// Consumed by Search.API to update the Elasticsearch document.
/// Price/Sku data should be consumed from SkuPriceChangedEvent instead.
/// </summary>
public sealed record ProductUpdatedEvent(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    bool IsActive,
    DateTime UpdatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
