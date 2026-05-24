using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Inventory.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles ProductUpdatedEvent from Catalog. On restart, the Catalog seeder
/// calls product.Update() (not Create), so this consumer must also create
/// inventory items if they don't exist yet — otherwise the Inventory DB
/// stays empty after a restart and ReserveStockCommandHandler fails.
/// </summary>
public sealed class ProductUpdatedConsumer(
    IInventoryItemRepository repository,
    IUnitOfWork uow,
    ILogger<ProductUpdatedConsumer> logger) : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var product = context.Message;
        logger.LogInformation("Processing ProductUpdatedEvent for SKU {Sku}", product.Sku);

        var existingItem = await repository.GetBySkuAsync(product.Sku);
        if (existingItem != null)
        {
            if (existingItem.ProductId != product.ProductId)
            {
                logger.LogInformation(
                    "Updating inventory ProductId for SKU {Sku}: {Old} → {New}",
                    product.Sku, existingItem.ProductId, product.ProductId);
                existingItem.UpdateProductId(product.ProductId);
                repository.Update(existingItem);
                await uow.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                logger.LogInformation("Inventory item already exists for SKU {Sku}", product.Sku);
            }
            return;
        }

        var inventoryItem = InventoryItem.Create(product.Sku, 0, product.StoreId, product.ProductId);
        repository.Add(inventoryItem);

        try
        {
            await uow.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Created new inventory record for SKU {Sku} via ProductUpdatedEvent", product.Sku);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Race: ProductCreatedConsumer inserted the same item concurrently.
            // Detach the failed entity so the EF Outbox's SaveChangesAsync
            // (which runs after this consumer returns) doesn't try to re-insert it.
            if (uow is DbContext dbContext)
                dbContext.Entry(inventoryItem).State = EntityState.Detached;
            logger.LogInformation("Inventory item for SKU {Sku} already created by concurrent consumer, skipping.", product.Sku);
        }
    }
}
