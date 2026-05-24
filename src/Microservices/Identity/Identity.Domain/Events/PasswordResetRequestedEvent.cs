using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Events;

public sealed record PasswordResetRequestedEvent(
    Guid UserId,
    string Email,
    string Token) : IDomainEvent;
