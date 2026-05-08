using System.Security.Claims;
using ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.UnitTests.Middleware;

public sealed class CsrfValidationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenMutatingRequestHasInvalidCsrfTokens_ShouldReturnForbidden()
    {
        var context = CreateAuthenticatedContext("POST");
        var nextCalled = false;
        var middleware = new CsrfValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("CSRF validation failed");
    }

    [Fact]
    public async Task InvokeAsync_WhenMutatingRequestHasMatchingCsrfTokens_ShouldCallNext()
    {
        var context = CreateAuthenticatedContext("POST");
        context.Request.Headers.Cookie = "XSRF-TOKEN=csrf-token";
        context.Request.Headers["X-XSRF-TOKEN"] = "csrf-token";

        var nextCalled = false;
        var middleware = new CsrfValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestMethodIsSafe_ShouldSkipCsrfValidation()
    {
        var context = CreateAuthenticatedContext("GET");
        var nextCalled = false;
        var middleware = new CsrfValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsUnauthenticated_ShouldSkipCsrfValidation()
    {
        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Method = "POST";

        var nextCalled = false;
        var middleware = new CsrfValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    private static HttpContext CreateAuthenticatedContext(string method)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "user-1")],
                    authenticationType: "Cookies")),
            Response =
            {
                Body = new MemoryStream()
            }
        };

        context.Request.Method = method;
        return context;
    }
}
