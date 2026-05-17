using System.Security.Claims;
using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.Commands.ChangePrice;
using Catalog.Application.Commands.CreateProduct;
using Catalog.Application.Commands.CreateReview;
using Catalog.Application.Commands.DeleteProduct;
using Catalog.Application.Commands.SellerResponse;
using Catalog.Application.Commands.UpdateProduct;
using Catalog.Application.Commands.VoteReview;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using Catalog.Application.Queries.GetProductReviews;
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

        // Public: featured products (for homepage)
        group.MapGet("/featured", async (
            [FromQuery] string? tag,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetFeaturedProductsQuery(tag), ct);
            return Results.Ok(result);
        })
        .WithName("GetFeaturedProducts")
        .Produces<List<ProductListDto>>();

        // Public: list products
        group.MapGet("/", async (
            ISender sender,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? storeId = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
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

        // Public: get product review summary
        group.MapGet("/{id:guid}/reviews/summary", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetReviewSummaryQuery(id), ct);
            return Results.Ok(result);
        })
        .WithName("GetReviewSummary")
        .Produces<ReviewSummaryDto>();

        // Public: get product reviews (paginated, filterable)
        group.MapGet("/{id:guid}/reviews", async (
            Guid id,
            ISender sender,
            [FromQuery] string? sort = null,
            [FromQuery] int? rating = null,
            [FromQuery] bool? photoOnly = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default) =>
        {
            var query = new GetProductReviewsQuery(
                id,
                page > 0 ? page : 1,
                pageSize > 0 ? Math.Min(pageSize, 50) : 10,
                sort ?? "helpful",
                rating,
                photoOnly);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetProductReviews")
        .Produces<PagedResult<ReviewDto>>();

        // Authorized: create review
        group.MapPost("/{id:guid}/reviews", async (
            Guid id,
            CreateReviewCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var userName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var cmd = command with { ProductId = id, UserId = Guid.Parse(userId), UserName = userName };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/products/{id}/reviews", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateReview")
        .RequireAuthorization()
        .Produces<ReviewDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Authorized: vote on review
        group.MapPost("/reviews/{reviewId:guid}/vote", async (
            Guid reviewId,
            VoteReviewCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var cmd = command with { ReviewId = reviewId, UserId = Guid.Parse(userId) };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("VoteReview")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Authorized: seller response to review (seller role only)
        group.MapPost("/reviews/{reviewId:guid}/response", async (
            Guid reviewId,
            SellerResponseCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            // Extract storeId from claims — sellers have StoreId in their claims
            var storeIdClaim = user.FindFirstValue("StoreId");
            if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out var storeId))
                return Results.Forbid();

            var cmd = command with { ReviewId = reviewId, StoreId = storeId };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("SellerResponse")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

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
