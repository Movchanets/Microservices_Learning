using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductUpdatedConsumer(
    CartDbContext dbContext,
    ILogger<ProductUpdatedConsumer> logger) : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product updated: {ProductId}, SKU={Sku}, Price={Price}",
            evt.ProductId, evt.Sku, evt.Price);

        try
        {
            var existing = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId || p.Sku == evt.Sku, context.CancellationToken);

            if (existing is not null)
            {
                existing.UpdateDetails(evt.Name, evt.Price, existing.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
                return;
            }

            logger.LogWarning("ProductPrice {ProductId} not found, creating from update event", evt.ProductId);

            dbContext.ProductPrices.Add(
                ProductPrice.Create(evt.ProductId, evt.Sku, evt.Name, evt.Price, "USD"));

            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race condition: clear tracker and retry as update
            dbContext.ChangeTracker.Clear();
            var retry = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId || p.Sku == evt.Sku, context.CancellationToken);
            if (retry is not null)
            {
                retry.UpdateDetails(evt.Name, evt.Price, retry.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                logger.LogWarning("ProductPrice {ProductId} could not be created or found after retry", evt.ProductId);
            }
        }
    }
}
