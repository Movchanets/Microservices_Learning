namespace BuildingBlocks.SharedContracts.Events.Identity;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime Timestamp);
