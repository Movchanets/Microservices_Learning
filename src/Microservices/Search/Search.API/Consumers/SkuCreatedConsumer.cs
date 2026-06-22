using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles SkuCreatedIntegrationEvent from Catalog.
/// Updates the product's price range, SKU count, and variant axes in the search index.
/// </summary>
public sealed class SkuCreatedConsumer(
    ISearchService searchService,
    ILogger<SkuCreatedConsumer> logger)
    : IConsumer<SkuCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SkuCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuCreatedEvent for SKU {SkuCode} on Product {ProductId}",
            msg.SkuCode, msg.ProductId);

        // Update price range and SKU count
        await searchService.AddSkuToProductAsync(
            msg.ProductId, msg.Price, msg.Currency, context.CancellationToken);

        // Update variant axes from SKU's typed attributes
        foreach (var (key, value) in msg.TypedAttributes)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                await searchService.AddVariantAxisValueAsync(
                    msg.ProductId, key, value, context.CancellationToken);
            }
        }

        logger.LogInformation(
            "Updated price range and variant axes for product {ProductId} from new SKU {SkuCode}",
            msg.ProductId, msg.SkuCode);
    }
}
