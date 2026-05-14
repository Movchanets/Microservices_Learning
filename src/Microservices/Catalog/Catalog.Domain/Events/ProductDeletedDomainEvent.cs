using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductDeletedDomainEvent(Guid ProductId) : IDomainEvent;
