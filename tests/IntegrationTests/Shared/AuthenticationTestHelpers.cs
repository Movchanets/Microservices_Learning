using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Marketplace.IntegrationTests.Shared;

/// <summary>
/// Shared authentication helpers for integration tests that host ASP.NET pipelines in-memory.
/// </summary>
public static class AuthenticationTestHelpers
{
    /// <summary>
    /// Creates an authenticated principal for test requests.
    /// </summary>
    public static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string userId = "user-1",
        string authenticationType = "Cookies") =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: authenticationType));

    /// <summary>
    /// Creates an authentication ticket containing an access token.
    /// </summary>
    public static AuthenticationTicket CreateTicketWithAccessToken(
        ClaimsPrincipal principal,
        string accessToken,
        string scheme = "Cookies")
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken
            {
                Name = "access_token",
                Value = accessToken
            }
        ]);

        return new AuthenticationTicket(principal, properties, scheme);
    }
}

/// <summary>
/// Test authentication service used to control AuthenticateAsync results in integration tests.
/// </summary>
public sealed class TestAuthenticationService(AuthenticateResult authenticateResult) : IAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
        Task.FromResult(authenticateResult);

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
        Task.CompletedTask;

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        Task.CompletedTask;
}
