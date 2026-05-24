using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductCreatedDomainEvent(
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
    DateTime CreatedAt) : IDomainEvent;
