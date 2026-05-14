using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductUpdatedDomainEvent(
    Guid ProductId,
    string Name) : IDomainEvent;
