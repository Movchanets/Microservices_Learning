using MediatR;

namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Marker interface for domain events dispatched within a bounded context.
/// </summary>
public interface IDomainEvent : INotification;
