using BuildingBlocks.SharedContracts.Events.Media;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Consumes MediaUploadedIntegrationEvent from Media.API.
/// Updates ProductSearchDocument.ImageUrl in Elasticsearch when a primary image
/// is uploaded for a Product, or for a SKU that carries a LinkedProductId.
///
/// This is the "initial upload" counterpart to MediaGalleryUpdatedConsumer,
/// which only handles gallery reorder/set-primary events.
///
/// When TargetType is "SKU" and LinkedProductId is set, we update the parent
/// product's ImageUrl — the first primary SKU image serves as the product
/// thumbnail in search results.
/// </summary>
public sealed class MediaUploadedConsumer(
    ISearchService searchService,
    ILogger<MediaUploadedConsumer> logger)
    : IConsumer<MediaUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<MediaUploadedIntegrationEvent> consumeContext)
    {
        var evt = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        // Only process primary uploads — non-primary images don't set the thumbnail
        if (!evt.IsPrimary) return;

        // Resolve the product ID based on target type
        Guid? productId = null;

        if (evt.TargetType.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            productId = evt.TargetId;
        }
        else if (evt.TargetType.Equals("SKU", StringComparison.OrdinalIgnoreCase)
                 && evt.LinkedProductId.HasValue)
        {
            productId = evt.LinkedProductId.Value;
        }

        if (productId is null)
        {
            logger.LogDebug(
                "MediaUploaded event for {TargetType}/{TargetId} has no product reference, skipping",
                evt.TargetType, evt.TargetId);
            return;
        }

        logger.LogInformation(
            "Updating ImageUrl for product {ProductId} from media upload ({TargetType}/{TargetId}): {Url}",
            productId, evt.TargetType, evt.TargetId, evt.Url);

        await searchService.UpdateProductImageUrlAsync(productId.Value, evt.Url, ct);

        logger.LogInformation(
            "Updated ProductSearchDocument.ImageUrl for {ProductId}", productId);
    }
}
