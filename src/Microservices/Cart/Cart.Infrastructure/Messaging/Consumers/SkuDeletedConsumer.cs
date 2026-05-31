using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SkuDeletedEvent from Catalog.
/// Removes the ProductPrice entry for the deleted SKU from the cart database.
///
/// Note: CartItem entries in active shopping carts are NOT removed here because
/// carts are stored in Redis and cannot be efficiently queried by SkuId.
/// Stale cart items will fail at checkout when ProductPrice lookup returns null.
/// </summary>
public sealed class SkuDeletedConsumer(
    CartDbContext dbContext,
    ILogger<SkuDeletedConsumer> logger) : IConsumer<SkuDeletedEvent>
{
    public async Task Consume(ConsumeContext<SkuDeletedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuDeletedEvent for SKU {SkuCode} (SkuId={SkuId})",
            msg.SkuCode, msg.SkuId);

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.SkuId == msg.SkuId, context.CancellationToken);

        if (existing is null)
        {
            logger.LogDebug("ProductPrice for SkuId {SkuId} not found, skipping", msg.SkuId);
            return;
        }

        dbContext.ProductPrices.Remove(existing);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Removed ProductPrice for SkuId {SkuId} (SKU {SkuCode} deleted)",
            msg.SkuId, msg.SkuCode);
    }
}
