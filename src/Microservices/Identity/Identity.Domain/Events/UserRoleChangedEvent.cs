using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Events;

public sealed record UserRoleChangedEvent(
    Guid UserId,
    string OldRole,
    string NewRole) : IDomainEvent;
