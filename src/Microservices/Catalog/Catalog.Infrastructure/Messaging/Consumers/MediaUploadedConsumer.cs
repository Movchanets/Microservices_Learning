using BuildingBlocks.SharedContracts.Events.Media;
using Catalog.Domain.Aggregates;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes MediaUploadedIntegrationEvent and updates Product.ImageUrl or Sku.ImageUrl
/// when a primary image is uploaded for a product or SKU.
///
/// This is part of the hybrid caching pattern:
///   - List views use the cached ImageUrl (no Media.API call needed)
///   - Detail pages fetch full gallery from Media.API via BFF
///
/// Only processes events where IsPrimary=true — non-primary uploads
/// don't change the cached thumbnail URL.
/// </summary>
public sealed class MediaUploadedConsumer(
    CatalogDbContext context,
    ILogger<MediaUploadedConsumer> logger)
    : IConsumer<MediaUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<MediaUploadedIntegrationEvent> consumeContext)
    {
        var evt = consumeContext.Message;

        // Only update cached ImageUrl for primary uploads
        if (!evt.IsPrimary) return;

        var ct = consumeContext.CancellationToken;

        logger.LogInformation(
            "Media uploaded for {TargetType}/{TargetId}, IsPrimary={IsPrimary}",
            evt.TargetType, evt.TargetId, evt.IsPrimary);

        // ── Update Product or SKU ImageUrl based on TargetType ───
        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == evt.TargetId, ct);

            if (product is not null)
            {
                product.SetImageUrl(evt.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Updated Product.ImageUrl for {ProductId}", evt.TargetId);
            }
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase))
        {
            var sku = await context.Skus
                .FirstOrDefaultAsync(s => s.Id == evt.TargetId, ct);

            if (sku is not null)
            {
                sku.SetImageUrl(evt.Url);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Updated Sku.ImageUrl for {SkuId}", evt.TargetId);
            }
        }
    }
}
