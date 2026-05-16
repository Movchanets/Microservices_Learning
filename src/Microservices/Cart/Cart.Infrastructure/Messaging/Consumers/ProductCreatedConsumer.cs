using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

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
            await dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        try
        {
            dbContext.ProductPrices.Add(
                ProductPrice.Create(evt.ProductId, evt.Sku, evt.Name, evt.Price, evt.Currency));
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Race condition: another message already inserted this product — update instead
            dbContext.ChangeTracker.Clear();
            var existingAfterRace = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);
            if (existingAfterRace is not null)
            {
                existingAfterRace.UpdateDetails(evt.Name, evt.Price, evt.Currency);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
        }
    }
}
