namespace BuildingBlocks.SharedContracts.Events.Identity;

/// <summary>
/// Published by Identity.API when a new user registers.
/// Consumed by StoreManagement (auto-creates seller store) and Notification (welcome email).
/// </summary>
/// <param name="UserId">Unique identifier of the newly registered user.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FirstName">User's first name.</param>
/// <param name="LastName">User's last name.</param>
/// <param name="Role">Assigned role (e.g. Buyer, Seller, Admin).</param>
/// <param name="Timestamp">When the registration occurred.</param>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime Timestamp);
