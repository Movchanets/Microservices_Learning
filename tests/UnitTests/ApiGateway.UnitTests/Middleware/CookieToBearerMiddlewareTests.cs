using System.Security.Claims;
using ApiGateway.Middleware;
using ApiGateway.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.UnitTests.Middleware;

public sealed class CookieToBearerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUserIsAuthenticatedAndTokenExists_ShouldSetAuthorizationHeader()
    {
        var context = CreateAuthenticatedContext("access-token-123");
        var nextCalled = false;
        var middleware = new CookieToBearerMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Request.Headers.Authorization.ToString().Should().Be("Bearer access-token-123");
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsAuthenticatedButTokenMissing_ShouldNotSetAuthorizationHeader()
    {
        var context = CreateAuthenticatedContext(accessToken: null);
        var middleware = new CookieToBearerMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Request.Headers.Authorization.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsUnauthenticated_ShouldSkipTokenLookup()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        var middleware = new CookieToBearerMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Request.Headers.Authorization.ToString().Should().BeEmpty();
    }

    private static HttpContext CreateAuthenticatedContext(string? accessToken)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1")],
                authenticationType: "Cookies"));

        var authProperties = new AuthenticationProperties();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            authProperties.StoreTokens(
            [
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = accessToken
                }
            ]);
        }

        var authTicket = new AuthenticationTicket(principal, authProperties, "Cookies");

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(
            new TestAuthenticationService(AuthenticateResult.Success(authTicket)));

        return new DefaultHttpContext
        {
            User = principal,
            RequestServices = services.BuildServiceProvider()
        };
    }
}
