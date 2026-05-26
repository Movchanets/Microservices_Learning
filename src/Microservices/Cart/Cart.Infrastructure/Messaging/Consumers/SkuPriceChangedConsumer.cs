using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SkuPriceChangedEvent from Catalog.
/// Updates the cached product price in the cart database.
/// </summary>
public sealed class SkuPriceChangedConsumer(
    CartDbContext dbContext,
    ILogger<SkuPriceChangedConsumer> logger) : IConsumer<SkuPriceChangedEvent>
{
    public async Task Consume(ConsumeContext<SkuPriceChangedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("SKU price changed: SKU {SkuCode} (SkuId={SkuId}), {OldPrice} -> {NewPrice} {Currency}",
            evt.SkuCode, evt.SkuId, evt.OldPrice, evt.NewPrice, evt.Currency);

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.SkuId == evt.SkuId, context.CancellationToken);

        if (existing is null)
        {
            logger.LogWarning("ProductPrice for SkuId {SkuId} not found for price change", evt.SkuId);
            return;
        }

        existing.UpdatePrice(evt.NewPrice, evt.Currency);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
