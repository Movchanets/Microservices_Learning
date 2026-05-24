using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IDomainEvent;
