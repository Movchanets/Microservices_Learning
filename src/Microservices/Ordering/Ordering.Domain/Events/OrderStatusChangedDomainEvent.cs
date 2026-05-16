using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Domain.Enumerations;

namespace Ordering.Domain.Events;

public sealed record OrderStatusChangedDomainEvent(
    Guid OrderId,
    string BuyerId,
    OrderStatus NewStatus,
    string? Notes) : IDomainEvent;
