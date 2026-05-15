using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Commands.CreateOrder;
using Ordering.Application.DTOs;
using Ordering.Application.Queries.GetOrderById;
using Ordering.Application.Queries.ListOrdersByBuyer;
using Ordering.Application.Queries.ListOrdersBySeller;

namespace Ordering.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", async (
            ClaimsPrincipal user,
            [FromBody] CreateOrderRequest request,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();
            var cmd = new CreateOrderCommand(buyerId, request.Items);
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value}", result.Value)
                : Results.BadRequest(new { result.Error });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error });
        });

        group.MapGet("/buyer/{buyerId}", async (
            string buyerId,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListOrdersByBuyerQuery(buyerId), ct);
            return Results.Ok(result.Value);
        });

        group.MapGet("/seller/{sellerId}", async (
            string sellerId,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListOrdersBySellerQuery(sellerId), ct);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization("Seller");
    }
}

public sealed record CreateOrderRequest(
    List<CreateOrderItemDto> Items);
