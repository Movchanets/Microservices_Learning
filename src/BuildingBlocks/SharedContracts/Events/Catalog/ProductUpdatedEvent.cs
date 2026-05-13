namespace BuildingBlocks.SharedContracts.Events.Catalog;

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
    bool IsActive,
    DateTime UpdatedAt);
