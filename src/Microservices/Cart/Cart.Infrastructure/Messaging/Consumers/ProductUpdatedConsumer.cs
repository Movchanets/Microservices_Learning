using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

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

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);

        if (existing is not null)
        {
            existing.UpdateDetails(evt.Name, evt.Price, existing.Currency);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        logger.LogWarning("ProductPrice {ProductId} not found, creating from update event", evt.ProductId);

        try
        {
            dbContext.ProductPrices.Add(
                ProductPrice.Create(evt.ProductId, evt.Sku, evt.Name, evt.Price, "USD"));
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            dbContext.ChangeTracker.Clear();
            var existingAfterRace = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);
            if (existingAfterRace is not null)
            {
                existingAfterRace.UpdateDetails(evt.Name, evt.Price, existingAfterRace.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
        }
    }
}
