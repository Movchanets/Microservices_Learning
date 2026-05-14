using ApiGateway.Contracts;
using ApiGateway.Helpers;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ApiGateway.Endpoints;

public static class BffEndpoints
{
    public static void MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/auth/login", async (
            LoginRequest request,
            IHttpClientFactory httpClientFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync("/api/identity/auth/login", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (authResponse is null)
            {
                return Results.Problem("Identity service returned an empty response.", statusCode: StatusCodes.Status502BadGateway);
            }

            await BffAuthHelpers.SignInAsync(ctx, authResponse);
            BffAuthHelpers.IssueCsrfCookie(ctx);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapPost("/bff/auth/forgot-password", async (
            ForgotPasswordRequest request,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync("/api/identity/auth/forgot-password", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);
            }

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapPost("/bff/auth/register", async (
            RegisterRequest request,
            IHttpClientFactory httpClientFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync("/api/identity/auth/register", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (authResponse is null)
            {
                return Results.Problem("Identity service returned an empty response.", statusCode: StatusCodes.Status502BadGateway);
            }

            await BffAuthHelpers.SignInAsync(ctx, authResponse);
            BffAuthHelpers.IssueCsrfCookie(ctx);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapPost("/bff/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync("Cookies");

            var isDevelopment = ctx.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
            ctx.Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Strict,
                Secure = !isDevelopment
            });

            return Results.NoContent();
        })
        .RequireAuthorization()
        .ExcludeFromDescription();

        app.MapGet("/bff/user", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub"),
                email = ctx.User.FindFirstValue("email") ?? ctx.User.FindFirstValue(ClaimTypes.Email),
                firstName = ctx.User.FindFirstValue("firstName"),
                lastName = ctx.User.FindFirstValue("lastName"),
                role = ctx.User.FindFirstValue(ClaimTypes.Role)
            });
        })
        .RequireAuthorization()
        .ExcludeFromDescription();

        app.MapGet("/bff/csrf", (HttpContext ctx) =>
        {
            BffAuthHelpers.IssueCsrfCookie(ctx);
            return Results.Ok();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // Health check moved to HealthEndpoints.cs — aggregated /bff/health
    }
}
