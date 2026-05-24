namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a new product is created.
/// Consumed by Search.API to index the product in Elasticsearch.
/// </summary>
public sealed record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
