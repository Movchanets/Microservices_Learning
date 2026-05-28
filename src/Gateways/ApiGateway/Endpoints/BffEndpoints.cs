using ApiGateway.Contracts;
using ApiGateway.Helpers;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authentication;
using System.Linq;
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
                role = string.Join(", ", ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value))
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

        // ── BFF Cart (enriched with product details) ────────
        app.MapGet("/bff/cart", async (
            ClaimsPrincipal user,
            CartBffService cartBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? user.FindFirstValue("sub");

            // Forward the Bearer token (set by CookieToBearerMiddleware) to downstream services
            var bearerToken = ctx.Request.Headers.Authorization.ToString();
            if (bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                bearerToken = bearerToken["Bearer ".Length..];

            // Forward X-Cart-Id for anonymous cart support
            var cartIdHeader = ctx.Request.Headers["X-Cart-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(buyerId) && string.IsNullOrEmpty(cartIdHeader))
                return Results.Ok(new CartDto(null, Guid.Empty, [], 0, 0));

            var cart = await cartBffService.GetCartWithDetailsAsync(buyerId, cartIdHeader, bearerToken, ct);
            return Results.Ok(cart);
        })
        .WithTags("Cart")
        .WithOpenApi();

        // ── BFF Orders (enriched with product details) ─────
        app.MapGet("/bff/orders/buyer/{buyerId}", async (
            string buyerId,
            OrderBffService orderBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var bearerToken = ctx.Request.Headers.Authorization.ToString();
            if (bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                bearerToken = bearerToken["Bearer ".Length..];

            var orders = await orderBffService.GetOrdersByBuyerAsync(buyerId, bearerToken, ct);
            return Results.Ok(orders);
        })
        .RequireAuthorization()
        .WithTags("Orders")
        .WithOpenApi();

        app.MapGet("/bff/orders/{id:guid}", async (
            Guid id,
            OrderBffService orderBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var bearerToken = ctx.Request.Headers.Authorization.ToString();
            if (bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                bearerToken = bearerToken["Bearer ".Length..];

            var order = await orderBffService.GetOrderByIdAsync(id, bearerToken, ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Orders")
        .WithOpenApi();

        // Health check moved to HealthEndpoints.cs — aggregated /bff/health

        // ── BFF Product (enriched with gallery) ─────────────
        app.MapGet("/bff/catalog/products/{id:guid}", async (
            Guid id,
            ProductBffService productBffService,
            CancellationToken ct) =>
        {
            var result = await productBffService.GetProductWithGalleryAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithTags("Catalog")
        .WithOpenApi();

        // ── BFF SKU Detail (enriched with gallery) ──────────
        app.MapGet("/bff/catalog/skus/{skuId:guid}", async (
            Guid skuId,
            ProductBffService productBffService,
            CancellationToken ct) =>
        {
            var result = await productBffService.GetSkuWithGalleryAsync(skuId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithTags("Catalog")
        .WithOpenApi();

        // ── BFF SKU Gallery (gallery only, lightweight) ─────
        app.MapGet("/bff/catalog/skus/{skuId:guid}/gallery", async (
            Guid skuId,
            ProductBffService productBffService,
            CancellationToken ct) =>
        {
            var gallery = await productBffService.GetSkuGalleryAsync(skuId, ct);
            return Results.Ok(gallery);
        })
        .WithTags("Catalog")
        .WithOpenApi();
    }
}
