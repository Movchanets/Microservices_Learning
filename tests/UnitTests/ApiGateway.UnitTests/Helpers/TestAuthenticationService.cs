using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ApiGateway.UnitTests.Helpers;

internal sealed class TestAuthenticationService(AuthenticateResult authenticateResult) : IAuthenticationService
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
