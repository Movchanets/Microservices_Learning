using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes MediaDeletedIntegrationEvent and clears Product.ImageUrl or Sku.ImageUrl
/// only if the deleted media was the primary image for that target.
/// </summary>
public sealed class MediaDeletedConsumer(
    CatalogDbContext context,
    ILogger<MediaDeletedConsumer> logger)
    : IConsumer<MediaDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<MediaDeletedIntegrationEvent> consumeContext)
    {
        var evt = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        logger.LogInformation(
            "Media deleted for {TargetType}/{TargetId}, WasPrimary={WasPrimary}",
            evt.TargetType, evt.TargetId, evt.WasPrimary);

        // Only clear ImageUrl if the deleted media was the primary image
        if (!evt.WasPrimary)
        {
            logger.LogDebug("Deleted media was not primary, skipping ImageUrl clear");
            return;
        }

        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == evt.TargetId, ct);
            if (product is not null && product.ImageUrl is not null)
            {
                product.SetImageUrl(null);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Cleared Product.ImageUrl for {ProductId}", evt.TargetId);
            }
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = await context.Skus.FirstOrDefaultAsync(s => s.Id == evt.TargetId, ct);
            if (sku is not null && sku.ImageUrl is not null)
            {
                sku.SetImageUrl(null);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Cleared Sku.ImageUrl for {SkuId}", evt.TargetId);
            }
        }
    }
}
