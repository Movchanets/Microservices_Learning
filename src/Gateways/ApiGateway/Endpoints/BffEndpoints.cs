using ApiGateway.Contracts;
using ApiGateway.Helpers;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authentication;
using System.Linq;
using System.Security.Claims;

namespace ApiGateway.Endpoints;

/// <summary>
/// BFF (Backend-for-Frontend) endpoints — session management, user info,
/// and aggregation endpoints that merge data from multiple microservices.
///
/// Auth endpoints (login/register/logout) are excluded from OpenAPI docs
/// because they're used by the Angular SPA via cookie-based sessions.
/// </summary>
public static class BffEndpoints
{
    public static void MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Auth: Login ──────────────────────────────────────────
        // POST /bff/auth/login → Identity API → set session cookie
        app.MapPost("/bff/auth/login", async (
            LoginRequest request,
            IHttpClientFactory httpClientFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync("/api/identity/auth/login", request, ct);

            if (!response.IsSuccessStatusCode)
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(
                cancellationToken: ct);
            if (authResponse is null)
                return Results.Problem(
                    "Identity service returned an empty response.",
                    statusCode: StatusCodes.Status502BadGateway);

            await BffAuthHelpers.SignInAsync(ctx, authResponse);
            BffAuthHelpers.IssueCsrfCookie(ctx);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // ── Auth: Forgot Password ────────────────────────────────
        // POST /bff/auth/forgot-password → Identity API
        app.MapPost("/bff/auth/forgot-password", async (
            ForgotPasswordRequest request,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync(
                "/api/identity/auth/forgot-password", request, ct);

            if (!response.IsSuccessStatusCode)
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // ── Auth: Register ───────────────────────────────────────
        // POST /bff/auth/register → Identity API → set session cookie
        app.MapPost("/bff/auth/register", async (
            RegisterRequest request,
            IHttpClientFactory httpClientFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var http = httpClientFactory.CreateClient("identity-api");
            var response = await http.PostAsJsonAsync(
                "/api/identity/auth/register", request, ct);

            if (!response.IsSuccessStatusCode)
                return await BffAuthHelpers.ToProblemResultAsync(response, ct);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(
                cancellationToken: ct);
            if (authResponse is null)
                return Results.Problem(
                    "Identity service returned an empty response.",
                    statusCode: StatusCodes.Status502BadGateway);

            await BffAuthHelpers.SignInAsync(ctx, authResponse);
            BffAuthHelpers.IssueCsrfCookie(ctx);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // ── Auth: Logout ─────────────────────────────────────────
        // POST /bff/auth/logout → clear session cookie + CSRF cookie
        app.MapPost("/bff/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync("Cookies");

            var isDevelopment = ctx.RequestServices
                .GetRequiredService<IHostEnvironment>().IsDevelopment();

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

        // ── User: Current User Info ──────────────────────────────
        // GET /bff/user → returns authenticated user's claims
        app.MapGet("/bff/user", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.User.FindFirstValue("sub"),
                email = ctx.User.FindFirstValue("email")
                  ?? ctx.User.FindFirstValue(ClaimTypes.Email),
                firstName = ctx.User.FindFirstValue("firstName"),
                lastName = ctx.User.FindFirstValue("lastName"),
                role = string.Join(", ",
                    ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value))
            });
        })
        .RequireAuthorization()
        .ExcludeFromDescription();

        // ── CSRF: Refresh Token ──────────────────────────────────
        // GET /bff/csrf → issues a new CSRF cookie
        app.MapGet("/bff/csrf", (HttpContext ctx) =>
        {
            BffAuthHelpers.IssueCsrfCookie(ctx);
            return Results.Ok();
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // ── Cart: Enriched with Product Details ──────────────────
        // GET /bff/cart → Cart API + Catalog API (product names, images)
        app.MapGet("/bff/cart", async (
            ClaimsPrincipal user,
            CartBffService cartBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? user.FindFirstValue("sub");

            var bearerToken = ExtractBearerToken(ctx);
            var cartIdHeader = ctx.Request.Headers["X-Cart-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(buyerId) && string.IsNullOrEmpty(cartIdHeader))
                return Results.Ok(new CartDto(null, Guid.Empty, [], 0, 0));

            var cart = await cartBffService.GetCartWithDetailsAsync(
                buyerId, cartIdHeader, bearerToken, ct);

            return Results.Ok(cart);
        })
        .WithTags("Cart")
        .WithOpenApi();

        // ── Orders: Enriched with Product Details ────────────────
        // GET /bff/orders/buyer/{buyerId} → Order API + Catalog API
        app.MapGet("/bff/orders/buyer/{buyerId}", async (
            string buyerId,
            OrderBffService orderBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var bearerToken = ExtractBearerToken(ctx);
            var orders = await orderBffService.GetOrdersByBuyerAsync(
                buyerId, bearerToken, ct);

            return Results.Ok(orders);
        })
        .RequireAuthorization()
        .WithTags("Orders")
        .WithOpenApi();

        // GET /bff/orders/{id} → Order API + Catalog API
        app.MapGet("/bff/orders/{id:guid}", async (
            Guid id,
            OrderBffService orderBffService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var bearerToken = ExtractBearerToken(ctx);
            var order = await orderBffService.GetOrderByIdAsync(id, bearerToken, ct);

            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Orders")
        .WithOpenApi();

        // ── Catalog: Product + Gallery (Parallel Fetch) ──────────
        // GET /bff/catalog/products/{id} → Catalog API + Media API
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

        // ── Catalog: SKU + Gallery (Parallel Fetch) ──────────────
        // GET /bff/catalog/skus/{skuId} → Catalog API + Media API
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

        // ── Catalog: SKU Gallery Only (Lightweight) ──────────────
        // GET /bff/catalog/skus/{skuId}/gallery → Media API only
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

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Extracts Bearer token from Authorization header.
    /// The CookieToBearerMiddleware sets this header from the session cookie.
    /// Returns null if no Bearer token is present.
    /// </summary>
    private static string? ExtractBearerToken(HttpContext ctx)
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..];
        return null;
    }
}
