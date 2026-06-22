using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles ProductDeletedEvent from Catalog.
/// Removes all ProductPrice entries for the deleted product, preventing
/// stale pricing data in the cart. Cart items referencing deleted products
/// will fail validation at checkout time.
/// </summary>
public sealed class ProductDeletedConsumer(
    CartDbContext dbContext,
    ILogger<ProductDeletedConsumer> logger) : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product deleted: {ProductId}", evt.ProductId);

        // Remove ALL ProductPrice entries for this product (one per SKU)
        var existing = await dbContext.ProductPrices
            .Where(p => p.ProductId == evt.ProductId)
            .ToListAsync(context.CancellationToken);

        if (existing.Count == 0)
        {
            logger.LogWarning("No ProductPrice entries found for deleted Product {ProductId}", evt.ProductId);
            return;
        }

        dbContext.ProductPrices.RemoveRange(existing);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Removed {Count} ProductPrice entries for deleted Product {ProductId}",
            existing.Count, evt.ProductId);
    }
}
