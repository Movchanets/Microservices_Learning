using MediatR;

namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Marker interface for domain events dispatched within a bounded context.
/// Rationale: Derives from MediatR's INotification so that domain events can be published
/// using MediatR's publish/subscribe mechanism easily across application layers.
/// </summary>
public interface IDomainEvent : INotification;
