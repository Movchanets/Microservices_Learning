using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductPriceChangedConsumer(
    CartDbContext dbContext,
    ILogger<ProductPriceChangedConsumer> logger) : IConsumer<ProductPriceChangedEvent>
{
    public async Task Consume(ConsumeContext<ProductPriceChangedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product price changed: {ProductId}, {OldPrice} -> {NewPrice}",
            evt.ProductId, evt.OldPrice, evt.NewPrice);

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);

        if (existing is null)
        {
            logger.LogWarning("ProductPrice {ProductId} not found for price change", evt.ProductId);
            return;
        }

        existing.UpdatePrice(evt.NewPrice, evt.Currency);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
