using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductDeletedConsumer(
    CartDbContext dbContext,
    ILogger<ProductDeletedConsumer> logger) : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product deleted: {ProductId}", evt.ProductId);

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == evt.ProductId, context.CancellationToken);

        if (existing is null)
        {
            logger.LogWarning("ProductPrice {ProductId} not found for deletion", evt.ProductId);
            return;
        }

        dbContext.ProductPrices.Remove(existing);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
