using System.Security.Claims;
using BuildingBlocks.SharedContracts.Dtos;
using Cart.Application.Commands;
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
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();
            var result = await sender.Send(new GetCartQuery(buyerId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (
            ClaimsPrincipal user,
            [FromBody] UpdateCartRequest request,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();
            var items = request.Items.Select(i => new CartItemDto(i.Sku, i.Quantity, i.Price)).ToList();
            var result = await sender.Send(new UpdateCartCommand(buyerId, items), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapDelete("/", async (
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();
            var result = await sender.Send(new DeleteCartCommand(buyerId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        group.MapPost("/checkout", async (
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();

            var result = await sender.Send(new CheckoutCartCommand(buyerId), ct);

            return result.IsSuccess ? Results.Accepted(value: result.Value) : Results.BadRequest(result.Error);
        });
    }
}

public record UpdateCartRequest(List<CartItemRequest> Items);
public record CartItemRequest(string Sku, int Quantity, decimal Price);