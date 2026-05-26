using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record SkuDeletedDomainEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode) : IDomainEvent;
