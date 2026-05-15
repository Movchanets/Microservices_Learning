using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Events;

public sealed record OrderCancelledDomainEvent(
    Guid OrderId,
    string BuyerId,
    string Reason) : IDomainEvent;
