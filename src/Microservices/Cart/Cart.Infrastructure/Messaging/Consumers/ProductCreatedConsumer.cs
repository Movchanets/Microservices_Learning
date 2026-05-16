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

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);

        if (existing is not null)
        {
            existing.UpdateDetails(evt.Name, evt.Price, evt.Currency);
        }
        else
        {
            // Check by SKU as well — handles the case where a different ProductId maps to the same SKU
            var existingBySku = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Sku == evt.Sku, context.CancellationToken);

            if (existingBySku is not null)
            {
                existingBySku.UpdateDetails(evt.Name, evt.Price, evt.Currency);
            }
            else
            {
                dbContext.ProductPrices.Add(
                    ProductPrice.Create(evt.ProductId, evt.Sku, evt.Name, evt.Price, evt.Currency));
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException) when (
            dbContext.ChangeTracker.Entries<ProductPrice>().Any(e => e.State == EntityState.Added))
        {
            // Race condition: clear tracker and retry as update
            dbContext.ChangeTracker.Clear();
            var retry = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId || p.Sku == evt.Sku, context.CancellationToken);
            if (retry is not null)
            {
                retry.UpdateDetails(evt.Name, evt.Price, evt.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
        }
    }
}
