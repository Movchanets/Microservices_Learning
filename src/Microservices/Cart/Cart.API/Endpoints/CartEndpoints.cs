using System.Security.Claims;
using Cart.Application.Commands;
using Cart.Application.Dtos;
using Cart.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart")
            .WithTags("Cart")
            .WithOpenApi();

        // GET /api/cart — anonymous OK
        group.MapGet("/", async (
            ClaimsPrincipal user,
            HttpRequest request,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var (buyerId, cartId) = GetCartIdentity(user, request);
            if (buyerId is null && cartId is null)
                return Results.Ok(new CartResponse(null, Guid.Empty, [], 0m, 0, DateTime.UtcNow));

            var result = await sender.Send(new GetCartQuery(buyerId, cartId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // DELETE /api/cart — anonymous OK
        group.MapDelete("/", async (
            ClaimsPrincipal user,
            HttpRequest request,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var (buyerId, cartId) = GetCartIdentity(user, request);
            if (buyerId is null && cartId is null)
                return Results.BadRequest("No cart identity provided.");

            var result = await sender.Send(new DeleteCartCommand(buyerId, cartId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        // POST /api/cart/checkout — AUTH REQUIRED
        group.MapPost("/checkout", async (
            ClaimsPrincipal user,
            HttpRequest request,
            [FromBody] CheckoutRequest req,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claim, out var buyerId))
                return Results.Unauthorized();

            // Extract X-Cart-Id for anonymous→authenticated cart merge
            Guid? cartId = null;
            if (request.Headers.TryGetValue("X-Cart-Id", out var cartIdHeader)
                && Guid.TryParse(cartIdHeader, out var parsedCartId))
                cartId = parsedCartId;

            var result = await sender.Send(new CheckoutCartCommand(
                buyerId,
                cartId,
                new AddressRequest(
                    req.AddressLine1,
                    req.AddressLine2,
                    req.City,
                    req.State,
                    req.PostalCode,
                    req.Country)), ct);

            return result.IsSuccess ? Results.Accepted(value: result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        // POST /api/cart/items — anonymous OK
        group.MapPost("/items", async (
            ClaimsPrincipal user,
            HttpRequest request,
            [FromBody] AddCartItemRequest req,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var (buyerId, cartId) = GetCartIdentity(user, request);
            var result = await sender.Send(new AddCartItemCommand(buyerId, cartId, req.ProductId, req.Quantity), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // PUT /api/cart/items/{productId} — anonymous OK
        group.MapPut("/items/{productId:guid}", async (
            ClaimsPrincipal user,
            HttpRequest request,
            Guid productId,
            [FromBody] UpdateCartItemRequest req,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var (buyerId, cartId) = GetCartIdentity(user, request);
            if (buyerId is null && cartId is null)
                return Results.BadRequest("No cart identity provided.");

            var result = await sender.Send(new UpdateCartItemCommand(buyerId, cartId, productId, req.Quantity), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // DELETE /api/cart/items/{productId} — anonymous OK
        group.MapDelete("/items/{productId:guid}", async (
            ClaimsPrincipal user,
            HttpRequest request,
            Guid productId,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var (buyerId, cartId) = GetCartIdentity(user, request);
            if (buyerId is null && cartId is null)
                return Results.BadRequest("No cart identity provided.");

            var result = await sender.Send(new RemoveCartItemCommand(buyerId, cartId, productId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    /// <summary>
    /// Extracts BuyerId from JWT claims (authenticated) and CartId from X-Cart-Id header (anonymous).
    /// </summary>
    private static (Guid? buyerId, Guid? cartId) GetCartIdentity(ClaimsPrincipal user, HttpRequest request)
    {
        Guid? buyerId = null;
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(claim, out var parsedBuyer))
            buyerId = parsedBuyer;

        Guid? cartId = null;
        if (request.Headers.TryGetValue("X-Cart-Id", out var cartIdHeader)
            && Guid.TryParse(cartIdHeader, out var parsedCartId))
            cartId = parsedCartId;

        return (buyerId, cartId);
    }
}

public record AddCartItemRequest(Guid ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);
public record CheckoutRequest(
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);
