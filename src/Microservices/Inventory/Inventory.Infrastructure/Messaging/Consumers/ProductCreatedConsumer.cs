using BuildingBlocks.SharedContracts.Events.Catalog;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class ProductCreatedConsumer(
    IInventoryItemRepository repository,
    BuildingBlocks.SharedContracts.Abstractions.IUnitOfWork uow,
    ILogger<ProductCreatedConsumer> logger) : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var product = context.Message;
        logger.LogInformation("Processing ProductCreatedEvent for SKU {Sku}", product.Sku);

        // Check if we already track this SKU to ensure idempotency
        var existingItem = await repository.GetBySkuAsync(product.Sku);
        if (existingItem != null)
        {
            logger.LogInformation("Inventory item already exists for SKU {Sku}", product.Sku);
            return;
        }

        // Create new inventory item with 0 stock
        var inventoryItem = InventoryItem.Create(product.Sku, 0);
        repository.Add(inventoryItem);
        
        await uow.SaveChangesAsync();
        
        logger.LogInformation("Created new inventory record for SKU {Sku}", product.Sku);
    }
}