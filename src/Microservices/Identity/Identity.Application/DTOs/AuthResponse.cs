namespace Identity.Application.DTOs;

/// <summary>
/// Data transfer object containing the result of a successful authentication operation.
/// </summary>
/// <param name="AccessToken">The JWT access token used for bearer authentication.</param>
/// <param name="RefreshToken">The opaque token used to obtain a new access token without re-authenticating.</param>
/// <param name="ExpiresAt">The UTC date and time when the access token expires.</param>
/// <param name="Email">The authenticated user's email address.</param>
/// <param name="Role">The authenticated user's assigned role.</param>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Email,
    string Role);
