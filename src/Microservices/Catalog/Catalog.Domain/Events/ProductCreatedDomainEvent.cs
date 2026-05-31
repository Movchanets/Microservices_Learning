using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductCreatedDomainEvent(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    List<string> Tags,
    string? ImageUrl,
    string? Brand,
    Guid StoreId,
    DateTime CreatedAt) : IDomainEvent;
