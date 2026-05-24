using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductUpdatedDomainEvent(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    bool IsActive,
    DateTime UpdatedAt) : IDomainEvent;
