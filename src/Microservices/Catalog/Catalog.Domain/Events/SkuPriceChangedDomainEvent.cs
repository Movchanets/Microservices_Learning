using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record SkuPriceChangedDomainEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : IDomainEvent;
