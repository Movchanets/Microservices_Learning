using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes MediaDeletedIntegrationEvent and clears Product.ImageUrl or Sku.ImageUrl
/// only if the deleted media was the primary image for that target.
///
/// Design: This consumer clears ImageUrl immediately. If there's a new primary image,
/// GalleryUpdatedConsumer will fire shortly after with the updated URL.
/// Worst case: ImageUrl is temporarily null between delete and gallery update.
///
/// The WasPrimary flag prevents unnecessary DB writes when deleting
/// non-primary images (e.g., removing the 3rd image from a gallery).
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

        // Only clear ImageUrl if the deleted media was the primary image.
        // Non-primary deletions don't affect the cached thumbnail.
        if (!evt.WasPrimary)
        {
            logger.LogDebug("Deleted media was not primary, skipping ImageUrl clear");
            return;
        }

        // ── Clear Product or SKU ImageUrl based on TargetType ────
        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == evt.TargetId, ct);

            if (product is not null && product.ImageUrl is not null)
            {
                product.SetImageUrl(null);
                await context.SaveChangesAsync(ct);
                logger.LogInformation(
                    "Cleared Product.ImageUrl for {ProductId}", evt.TargetId);
            }
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = await context.Skus
                .FirstOrDefaultAsync(s => s.Id == evt.TargetId, ct);

            if (sku is not null && sku.ImageUrl is not null)
            {
                sku.SetImageUrl(null);
                await context.SaveChangesAsync(ct);
                logger.LogInformation(
                    "Cleared Sku.ImageUrl for {SkuId}", evt.TargetId);
            }
        }
    }
}
