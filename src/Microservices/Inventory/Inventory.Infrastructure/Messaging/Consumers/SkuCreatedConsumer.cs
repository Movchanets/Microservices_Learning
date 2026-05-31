using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SkuCreatedIntegrationEvent from Catalog.
/// Creates an InventoryItem (qty=0) for the new SKU.
/// This is the PRIMARY consumer for new SKU creation.
/// </summary>
public sealed class SkuCreatedConsumer(
    IInventoryItemRepository repository,
    IUnitOfWork uow,
    ILogger<SkuCreatedConsumer> logger) : IConsumer<SkuCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SkuCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuCreatedEvent for SKU {SkuCode} (SkuId={SkuId})",
            msg.SkuCode, msg.SkuId);

        // Check if already exists by SkuId
        var existing = await repository.GetBySkuIdAsync(msg.SkuId, context.CancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Inventory item already exists for SkuId {SkuId}", msg.SkuId);
            return;
        }

        var inventoryItem = InventoryItem.Create(
            msg.SkuId, msg.ProductId, msg.SkuCode, 0, msg.StoreId);
        repository.Add(inventoryItem);

        await uow.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Created inventory record for SKU {SkuCode} (qty=0)", msg.SkuCode);
    }
}
