using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductCreatedConsumer(
    CartDbContext dbContext,
    ILogger<ProductCreatedConsumer> logger) : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product created: {ProductId}, SKU={Sku}, Price={Price}",
            evt.ProductId, evt.Sku, evt.Price);

        try
        {
            var existing = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId || p.Sku == evt.Sku, context.CancellationToken);

            if (existing is not null)
            {
                existing.UpdateDetails(evt.Name, evt.Price, evt.Currency);
            }
            else
            {
                dbContext.ProductPrices.Add(
                    ProductPrice.Create(evt.ProductId, evt.Sku, evt.Name, evt.Price, evt.Currency));
            }

            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race condition: concurrent consumer inserted first. Clear tracker and retry as update.
            dbContext.ChangeTracker.Clear();
            var retry = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId || p.Sku == evt.Sku, context.CancellationToken);
            if (retry is not null)
            {
                retry.UpdateDetails(evt.Name, evt.Price, evt.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            else
            {
                logger.LogWarning("ProductPrice {ProductId} could not be created or found after retry", evt.ProductId);
            }
        }
    }
}
