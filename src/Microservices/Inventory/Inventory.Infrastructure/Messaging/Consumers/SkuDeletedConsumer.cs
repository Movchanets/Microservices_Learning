using BuildingBlocks.SharedContracts.Events.Catalog;
using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SkuDeletedEvent from Catalog.
/// Deactivates the corresponding InventoryItem (zeros AvailableQuantity)
/// so no new reservations can be made for the deleted SKU.
/// </summary>
public sealed class SkuDeletedConsumer(
    IInventoryItemRepository repository,
    IUnitOfWork uow,
    ILogger<SkuDeletedConsumer> logger) : IConsumer<SkuDeletedEvent>
{
    public async Task Consume(ConsumeContext<SkuDeletedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuDeletedEvent for SKU {SkuCode} (SkuId={SkuId})",
            msg.SkuCode, msg.SkuId);

        var item = await repository.GetBySkuIdAsync(msg.SkuId, context.CancellationToken);
        if (item is null)
        {
            logger.LogWarning("Inventory item not found for deleted SkuId {SkuId}, skipping", msg.SkuId);
            return;
        }

        item.Deactivate();
        repository.Update(item);
        await uow.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Deactivated inventory for SKU {SkuCode} (AvailableQty set to 0)", msg.SkuCode);
    }
}
