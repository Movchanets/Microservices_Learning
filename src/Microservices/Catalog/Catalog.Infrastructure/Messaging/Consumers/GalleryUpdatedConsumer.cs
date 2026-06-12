using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes GalleryUpdatedIntegrationEvent and updates Product.ImageUrl or Sku.ImageUrl
/// from the primary gallery item.
///
/// This handles two scenarios:
///   1. Gallery reorder — the primary image changed position
///   2. SetPrimary — a different image was marked as primary
///
/// In both cases, we update the cached ImageUrl so list views show
/// the correct thumbnail without calling Media.API.
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

        // Find the primary item in the gallery update
        var primaryItem = evt.Items.FirstOrDefault(i => i.IsPrimary);
        if (primaryItem is null)
        {
            logger.LogDebug(
                "No primary item in gallery update for {TargetType}/{TargetId}",
                evt.TargetType, evt.TargetId);
            return;
        }

        logger.LogInformation(
            "Gallery updated for {TargetType}/{TargetId}, primary={PrimaryUrl}",
            evt.TargetType, evt.TargetId, primaryItem.Url);

        // ── Update Product or SKU ImageUrl based on TargetType ───
        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == evt.TargetId, ct);

            if (product is not null)
            {
                product.SetImageUrl(primaryItem.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation(
                    "Updated Product.ImageUrl from gallery for {ProductId}", evt.TargetId);
            }
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = await context.Skus
                .FirstOrDefaultAsync(s => s.Id == evt.TargetId, ct);

            if (sku is not null)
            {
                sku.SetImageUrl(primaryItem.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation(
                    "Updated Sku.ImageUrl from gallery for {SkuId}", evt.TargetId);

                // Propagate first primary SKU image to parent product's ImageUrl
                // (used by list views / product cards that don't fetch full gallery)
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == sku.ProductId, ct);

                if (product is not null
                    && (string.IsNullOrEmpty(product.ImageUrl)
                        || !product.ImageUrl.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase)))
                {
                    product.SetImageUrl(primaryItem.Url);
                    await context.SaveChangesAsync(ct);
                    logger.LogInformation(
                        "Propagated SKU gallery image to Product.ImageUrl for {ProductId}", product.Id);
                }
            }
        }
    }
}
