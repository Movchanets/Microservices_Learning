namespace ApiGateway.Contracts;

internal sealed record LoginRequest(string Email, string Password);
internal sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string Email, string Role);
