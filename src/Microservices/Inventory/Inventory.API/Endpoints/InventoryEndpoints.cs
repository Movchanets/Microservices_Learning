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
            var item = InventoryItem.Create(request.Sku, request.InitialQuantity);
            repository.Add(item);
            await uow.SaveChangesAsync(ct);
            return Results.Created($"/api/inventory/items/{item.Id}", item.Id);
        })
        .RequireAuthorization();

        group.MapPost("/items/{sku}/add-stock", async (
            string sku,
            [FromBody] AddStockRequest request,
            [FromServices] IInventoryItemRepository repository,
            [FromServices] IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuAsync(sku, ct);
            if (item == null) return Results.NotFound();

            item.AddStock(request.Quantity);
            repository.Update(item);
            await uow.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .RequireAuthorization();

        group.MapGet("/items/{sku}", async (
            string sku,
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuAsync(sku, ct);
            return item == null ? Results.NotFound() : Results.Ok(new { item.Sku, item.AvailableQuantity });
        });

        group.MapGet("/items", async (
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.GetAllAsync(ct);
            return Results.Ok(items.Select(i => new { i.Id, i.Sku, i.AvailableQuantity }));
        })
        .RequireAuthorization();

        // Batch lookup by SKUs — for seller inventory dashboard
        group.MapPost("/items/batch", async (
            [FromBody] BatchInventoryRequest request,
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.GetBySkusAsync(request.Skus, ct);
            return Results.Ok(items.Select(i => new { i.Id, i.Sku, i.AvailableQuantity }));
        })
        .RequireAuthorization();
    }
}

public record CreateInventoryItemRequest(string Sku, int InitialQuantity);
public record AddStockRequest(int Quantity);
public record BatchInventoryRequest(List<string> Skus);