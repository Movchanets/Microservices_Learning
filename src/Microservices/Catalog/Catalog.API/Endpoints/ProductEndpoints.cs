using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.Commands.ChangePrice;
using Catalog.Application.Commands.CreateProduct;
using Catalog.Application.Commands.DeleteProduct;
using Catalog.Application.Commands.UpdateProduct;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/products")
            .WithTags("Products")
            .WithOpenApi();

        // Public: list products
        group.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? storeId,
            [FromQuery] string? search,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListProductsQuery(
                page > 0 ? page : 1,
                pageSize > 0 ? Math.Min(pageSize, 100) : 20,
                categoryId, storeId, search);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("ListProducts")
        .Produces<PagedResult<ProductListDto>>();

        // Public: get product by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var product = await sender.Send(new GetProductByIdQuery(id), ct);
            return product is not null
                ? Results.Ok(product)
                : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<ProductDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Authorized: create product (seller/admin)
        group.MapPost("/", async (
            CreateProductCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/products/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateProduct")
        .RequireAuthorization()
        .Produces<ProductDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Authorized: update product
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            // Ensure route ID matches command
            var cmd = command with { ProductId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("UpdateProduct")
        .RequireAuthorization()
        .Produces<ProductDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Authorized: change price
        group.MapPatch("/{id:guid}/price", async (
            Guid id,
            ChangePriceCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { ProductId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("ChangeProductPrice")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Public: get product recommendations (same category)
        group.MapGet("/{id:guid}/recommendations", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductRecommendationsQuery(id), ct);
            return Results.Ok(result);
        })
        .WithName("GetProductRecommendations")
        .Produces<List<ProductListDto>>();

        // Authorized: soft-delete product
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
