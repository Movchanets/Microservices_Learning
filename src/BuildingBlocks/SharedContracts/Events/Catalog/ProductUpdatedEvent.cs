namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when product details are modified.
/// Consumed by Search.API to update the Elasticsearch document.
/// </summary>
public sealed record ProductUpdatedEvent(
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
    bool IsActive,
    DateTime UpdatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
