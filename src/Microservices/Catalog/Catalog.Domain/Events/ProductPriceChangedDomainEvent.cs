using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record ProductPriceChangedDomainEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : IDomainEvent;
