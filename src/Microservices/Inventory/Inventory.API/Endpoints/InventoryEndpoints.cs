using Inventory.Domain.Aggregates;
using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Inventory.API.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .WithOpenApi();

        group.MapPost("/items", async (
            [FromBody] CreateInventoryItemRequest request,
            [FromServices] IInventoryItemRepository repository,
            [FromServices] IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var item = InventoryItem.Create(request.SkuId, request.ProductId, request.SkuCode, request.InitialQuantity, request.StoreId);
            repository.Add(item);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/api/inventory/items/{item.Id}", item.Id);
        })
        .RequireAuthorization();

        group.MapPost("/items/{skuCode}/add-stock", async (
            string skuCode,
            [FromBody] AddStockRequest request,
            [FromServices] IInventoryItemRepository repository,
            [FromServices] IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuCodeAsync(skuCode, ct);
            if (item == null) return Results.NotFound();

            item.AddStock(request.Quantity);
            repository.Update(item);
            await uow.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .RequireAuthorization();

        group.MapGet("/items/{skuCode}", async (
            string skuCode,
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuCodeAsync(skuCode, ct);
            return item == null ? Results.NotFound() : Results.Ok(new { item.SkuCode, item.AvailableQuantity });
        });

        group.MapGet("/items", async (
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.GetAllAsync(ct);
            return Results.Ok(items.Select(i => new { i.Id, i.SkuCode, i.AvailableQuantity }));
        })
        .RequireAuthorization();

        // Batch lookup by SKU IDs — for seller inventory dashboard
        group.MapPost("/items/batch", async (
            [FromBody] BatchInventoryRequest request,
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.GetBySkuIdsAsync(request.SkuIds, ct);
            return Results.Ok(items.Select(i => new { i.Id, i.SkuCode, i.AvailableQuantity }));
        })
        .RequireAuthorization();

        // Idempotent: upsert stock quantity by SKU code (creates item if not exists)
        group.MapPut("/items/{skuCode}/stock", async (
            string skuCode,
            [FromBody] SetStockRequest request,
            [FromServices] IInventoryItemRepository repository,
            [FromServices] IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuCodeAsync(skuCode, ct);
            if (item is null)
            {
                // Generate a deterministic SkuId from the code if not provided
                var skuId = request.SkuId != Guid.Empty
                    ? request.SkuId
                    : Guid.CreateVersion7();
                item = InventoryItem.Create(skuId, request.ProductId, skuCode, request.Quantity, request.StoreId);
                repository.Add(item);
            }
            else
            {
                var diff = request.Quantity - item.AvailableQuantity;
                if (diff > 0) item.AddStock(diff);
            }

            await uow.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .RequireAuthorization();
    }
}

public record CreateInventoryItemRequest(Guid SkuId, Guid ProductId, string SkuCode, int InitialQuantity, Guid StoreId);
public record AddStockRequest(int Quantity);
public record SetStockRequest(Guid SkuId, int Quantity, Guid StoreId, Guid ProductId);
public record BatchInventoryRequest(List<Guid> SkuIds);
