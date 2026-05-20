namespace BuildingBlocks.SharedContracts.Events.Identity;

/// <summary>
/// Published by Identity.API when a user requests a password reset.
/// Consumed by Notification service to send the reset email.
/// </summary>
/// <param name="UserId">Unique identifier of the user requesting the reset.</param>
/// <param name="Email">User's email address.</param>
/// <param name="Token">The generated password reset token.</param>
/// <param name="Timestamp">When the reset was requested.</param>
public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime Timestamp);
