using System.Net;
using System.Net.Http.Json;
using ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Marketplace.IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.IntegrationTests;

public sealed class MiddlewarePipelineTests
{
    [Fact]
    public async Task AuthenticatedPost_WithValidCsrfAndAccessToken_ShouldForwardBearerHeader()
    {
        await using var app = await CreateTestAppAsync("access-token-123");
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/proxy");
        request.Headers.Add("Cookie", "XSRF-TOKEN=csrf-token-1");
        request.Headers.Add("X-XSRF-TOKEN", "csrf-token-1");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ProxyResponse>();
        payload.Should().NotBeNull();
        payload!.Authorization.Should().Be("Bearer access-token-123");
    }

    [Fact]
    public async Task AuthenticatedPost_WithoutCsrfHeader_ShouldReturnForbidden()
    {
        await using var app = await CreateTestAppAsync("access-token-123");
        var client = app.GetTestClient();

        var response = await client.PostAsync("/proxy", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("CSRF validation failed");
    }

    private static async Task<WebApplication> CreateTestAppAsync(string accessToken)
    {
        var principal = AuthenticationTestHelpers.CreateAuthenticatedPrincipal();
        var authTicket = AuthenticationTestHelpers.CreateTicketWithAccessToken(principal, accessToken);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IAuthenticationService>(
            new TestAuthenticationService(AuthenticateResult.Success(authTicket)));

        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.User = principal;
            return next();
        });
        app.UseMiddleware<CsrfValidationMiddleware>();
        app.UseMiddleware<CookieToBearerMiddleware>();

        app.MapPost("/proxy", (HttpContext context) =>
            Results.Ok(new ProxyResponse(context.Request.Headers.Authorization.ToString())));

        await app.StartAsync();
        return app;
    }

    private sealed record ProxyResponse(string Authorization);
}
