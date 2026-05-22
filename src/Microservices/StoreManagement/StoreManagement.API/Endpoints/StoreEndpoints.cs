using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Application.Commands.CreateStore;
using StoreManagement.Application.Commands.SetStoreLogo;
using StoreManagement.Application.Commands.UpdateStore;
using StoreManagement.Application.Commands.VerifySeller;
using StoreManagement.Application.DTOs;
using StoreManagement.Application.Queries.GetStoreById;
using StoreManagement.Application.Queries.GetStoreBySellerId;
using StoreManagement.Application.Queries.ListStores;

namespace StoreManagement.API.Endpoints;

public static class StoreEndpoints
{
    public static void MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Stores")
            .WithOpenApi();

        // Create store (authenticated — seller)
        group.MapPost("/", async (
            CreateStoreCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/stores/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateStore")
        .RequireAuthorization("Seller")
        .Produces<StoreDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // List all stores (public, optional status filter)
        group.MapGet("/", async (
            [FromQuery] string? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListStoresQuery(status), ct);
            return Results.Ok(result.Value);
        })
        .WithName("ListStores")
        .Produces<IReadOnlyList<StoreListDto>>();

        // Get store by ID (public)
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStoreByIdQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error });
        })
        .WithName("GetStoreById")
        .Produces<StoreDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Get store by seller ID (public)
        group.MapGet("/seller/{sellerId}", async (
            string sellerId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStoreBySellerIdQuery(sellerId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error });
        })
        .WithName("GetStoreBySellerId")
        .Produces<StoreDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Update store (authenticated — owner)
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateStoreCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { StoreId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("UpdateStore")
        .RequireAuthorization("Seller")
        .Produces<StoreDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Verify seller (admin only)
        group.MapPost("/{id:guid}/verify", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new VerifySellerCommand(id, true, null), ct);
            return result.IsSuccess
                ? Results.Ok(new { StoreId = result.Value })
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("VerifySeller")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Set store logo (authenticated — owner)
        group.MapPut("/{id:guid}/logo", async (
            Guid id,
            SetStoreLogoCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { StoreId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(new { StoreId = result.Value })
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("SetStoreLogo")
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
