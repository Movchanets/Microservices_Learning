using System.Security.Claims;
using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.Commands.ActivateProduct;
using Catalog.Application.Commands.AddSku;
using Catalog.Application.Commands.BulkAddSku;
using Catalog.Application.Commands.ChangePrice;
using Catalog.Application.Commands.CreateProduct;
using Catalog.Application.Commands.CreateReview;
using Catalog.Application.Commands.DeactivateProduct;
using Catalog.Application.Commands.DeleteProduct;
using Catalog.Application.Commands.RemoveSku;
using Catalog.Application.Commands.SellerResponse;
using Catalog.Application.Commands.UpdateProduct;
using Catalog.Application.Commands.VoteReview;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using Catalog.Application.Queries.GetProductReviews;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Endpoints;

/// <summary>
/// Catalog API endpoints for products, SKUs, and reviews.
/// All business logic is delegated to MediatR handlers — endpoints only
/// handle HTTP concerns (routing, auth, response mapping).
///
/// Endpoint groups:
///   /api/catalog/products           — Product CRUD + search
///   /api/catalog/products/{id}/skus — SKU management
///   /api/catalog/products/{id}/reviews — Reviews
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/products")
            .WithTags("Products")
            .WithOpenApi();

        group.MapProductCrudEndpoints();
        group.MapProductReviewEndpoints();
        group.MapProductSkuEndpoints();
    }

    // ════════════════════════════════════════════════════════════════
    // PRODUCT CRUD
    // ════════════════════════════════════════════════════════════════

    private static void MapProductCrudEndpoints(this RouteGroupBuilder group)
    {
        // ── Public: Featured Products (Homepage) ─────────────────
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

        // ── Public: Batch Lookup by IDs (BFF Cart Enrichment) ───
        group.MapPost("/by-ids", async (
            List<Guid> ids,
            ISender sender,
            CancellationToken ct) =>
        {
            if (ids.Count == 0)
                return Results.Ok(new List<ProductListDto>());
            if (ids.Count > 100)
                return Results.BadRequest(new { error = "Maximum 100 product IDs allowed." });

            var result = await sender.Send(new GetProductsByIdsQuery(ids), ct);
            return Results.Ok(result);
        })
        .WithName("GetProductsByIds")
        .Produces<List<ProductListDto>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Public: Get Product by SKU Code ──────────────────────
        group.MapGet("/sku/{sku}", async (
            string sku,
            ISender sender,
            CancellationToken ct) =>
        {
            var product = await sender.Send(new GetProductBySkuQuery(sku), ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetProductBySku")
        .Produces<ProductDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Public: List Products (Paginated, Filterable) ────────
        group.MapGet("/", async (
            ISender sender,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? storeId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var query = new ListProductsQuery(
                page > 0 ? page : 1,
                pageSize > 0 ? Math.Min(pageSize, 100) : 20,
                categoryId, storeId, search, status);

            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("ListProducts")
        .Produces<PagedResult<ProductListDto>>();

        // ── Public: Get Product by ID ────────────────────────────
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var product = await sender.Send(new GetProductByIdQuery(id), ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<ProductDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Authorized: Create Product (Seller/Admin) ────────────
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

        // ── Authorized: Update Product ───────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
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

        // ── Authorized: Change Price ─────────────────────────────
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

        // ── Public: Product Recommendations (Same Category) ──────
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

        // ── Public: Variant Matrix (for variant picker) ─────────
        group.MapGet("/{id:guid}/variant-matrix", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetVariantMatrixQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetVariantMatrix")
        .Produces<VariantMatrixDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Authorized: Activate Product ─────────────────────────
        group.MapPut("/{id:guid}/activate", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ActivateProductCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        })
        .WithName("ActivateProduct")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Authorized: Deactivate Product ───────────────────────
        group.MapPut("/{id:guid}/deactivate", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateProductCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeactivateProduct")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Authorized: Soft-Delete Product ──────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    // ════════════════════════════════════════════════════════════════
    // PRODUCT REVIEWS
    // ════════════════════════════════════════════════════════════════

    private static void MapProductReviewEndpoints(this RouteGroupBuilder group)
    {
        // ── Public: Review Summary (Rating Distribution) ─────────
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

        // ── Public: List Reviews (Paginated, Filterable) ─────────
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

        // ── Authorized: Create Review ────────────────────────────
        group.MapPost("/{id:guid}/reviews", async (
            Guid id,
            CreateReviewCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var userName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var cmd = command with
            {
                ProductId = id,
                UserId = Guid.Parse(userId),
                UserName = userName
            };

            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/products/{id}/reviews", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateReview")
        .RequireAuthorization()
        .Produces<ReviewDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Authorized: Vote on Review (Helpful/Not Helpful) ─────
        group.MapPost("/reviews/{reviewId:guid}/vote", async (
            Guid reviewId,
            VoteReviewCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

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

        // ── Authorized: Seller Response to Review ────────────────
        group.MapPost("/reviews/{reviewId:guid}/response", async (
            Guid reviewId,
            SellerResponseCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            // Extract storeId from claims — sellers have StoreId in their JWT
            var storeIdClaim = user.FindFirstValue("StoreId");
            if (string.IsNullOrEmpty(storeIdClaim)
                || !Guid.TryParse(storeIdClaim, out var storeId))
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
    }

    // ════════════════════════════════════════════════════════════════
    // SKU MANAGEMENT
    // ════════════════════════════════════════════════════════════════

    private static void MapProductSkuEndpoints(this RouteGroupBuilder group)
    {
        // ── Public: Get SKU by ID ────────────────────────────────
        group.MapGet("/skus/{skuId:guid}", async (
            Guid skuId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSkuByIdQuery(skuId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetSkuById")
        .WithOpenApi()
        .Produces<SkuDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Authorized: Add SKU to Product ───────────────────────
        group.MapPost("/{id:guid}/skus", async (
            Guid id,
            AddSkuCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { ProductId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created(
                    $"/api/catalog/products/{id}/skus/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("AddSku")
        .RequireAuthorization()
        .Produces<SkuDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Authorized: Bulk Add SKUs (Variant Combinations) ───
        group.MapPost("/{id:guid}/skus/bulk", async (
            Guid id,
            BulkAddSkuCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { ProductId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/products/{id}/skus", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("BulkAddSkus")
        .RequireAuthorization()
        .Produces<BulkAddSkuResultDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Authorized: Remove SKU from Product ──────────────────
        group.MapDelete("/{id:guid}/skus/{skuId:guid}", async (
            Guid id,
            Guid skuId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveSkuCommand(id, skuId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("RemoveSku")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Authorized: Change SKU Price ─────────────────────────
        group.MapPatch("/{id:guid}/skus/{skuId:guid}/price", async (
            Guid id,
            Guid skuId,
            ChangePriceCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { ProductId = id, SkuId = skuId };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("ChangeSkuPrice")
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
