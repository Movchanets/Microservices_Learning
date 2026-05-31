using BuildingBlocks.SharedContracts.Events.Media;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Consumes GalleryUpdatedIntegrationEvent from Media.API.
/// Updates ProductSearchDocument.ImageUrl in Elasticsearch when the primary image changes.
///
/// This ensures search results show the correct thumbnail without waiting for
/// a Catalog domain event to propagate. It's a "fast path" — the Catalog consumer
/// (GalleryUpdatedConsumer) handles the canonical Product.ImageUrl update,
/// but this consumer keeps Elasticsearch in sync directly from Media events.
///
/// Only handles Product targets — SKU-level images don't appear in search results.
/// </summary>
public sealed class MediaGalleryUpdatedConsumer(
    ISearchService searchService,
    ILogger<MediaGalleryUpdatedConsumer> logger)
    : IConsumer<GalleryUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<GalleryUpdatedIntegrationEvent> consumeContext)
    {
        var evt = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        // Only handle Product targets — SKU-level images don't appear in search results
        if (!evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
            return;

        var primaryItem = evt.Items.FirstOrDefault(i => i.IsPrimary);
        var primaryUrl = primaryItem?.Url;

        logger.LogInformation(
            "Gallery updated for product {ProductId}, primary={PrimaryUrl}",
            evt.TargetId, primaryUrl ?? "(none)");

        await searchService.UpdateProductImageUrlAsync(evt.TargetId, primaryUrl, ct);

        logger.LogInformation(
            "Updated ProductSearchDocument.ImageUrl for {ProductId}", evt.TargetId);
    }
}
