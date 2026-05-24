using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Events;

public sealed record OrderCompletedDomainEvent(
    Guid OrderId,
    string BuyerId) : IDomainEvent;
