using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes GalleryUpdatedIntegrationEvent and updates Product.ImageUrl or Sku.ImageUrl
/// from the primary gallery item.
/// </summary>
public sealed class GalleryUpdatedConsumer(
    CatalogDbContext context,
    ILogger<GalleryUpdatedConsumer> logger)
    : IConsumer<GalleryUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<GalleryUpdatedIntegrationEvent> consumeContext)
    {
        var evt = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        var primaryItem = evt.Items.FirstOrDefault(i => i.IsPrimary);
        if (primaryItem is null)
        {
            logger.LogDebug("No primary item in gallery update for {TargetType}/{TargetId}", evt.TargetType, evt.TargetId);
            return;
        }

        logger.LogInformation(
            "Gallery updated for {TargetType}/{TargetId}, primary={PrimaryUrl}",
            evt.TargetType, evt.TargetId, primaryItem.Url);

        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == evt.TargetId, ct);
            if (product is not null)
            {
                product.SetImageUrl(primaryItem.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Updated Product.ImageUrl from gallery for {ProductId}", evt.TargetId);
            }
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = await context.Skus.FirstOrDefaultAsync(s => s.Id == evt.TargetId, ct);
            if (sku is not null)
            {
                sku.SetImageUrl(primaryItem.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Updated Sku.ImageUrl from gallery for {SkuId}", evt.TargetId);
            }
        }
    }
}
