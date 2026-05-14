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
        });

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
        });
        
        group.MapGet("/items/{sku}", async (
            string sku,
            [FromServices] IInventoryItemRepository repository,
            CancellationToken ct) =>
        {
            var item = await repository.GetBySkuAsync(sku, ct);
            return item == null ? Results.NotFound() : Results.Ok(new { item.Sku, item.AvailableQuantity });
        });
    }
}

public record CreateInventoryItemRequest(string Sku, int InitialQuantity);
public record AddStockRequest(int Quantity);