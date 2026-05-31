using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles SkuCreatedIntegrationEvent from Catalog.
/// Updates the product's price range and SKU count in the search index.
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

        await searchService.AddSkuToProductAsync(
            msg.ProductId, msg.Price, msg.Currency, context.CancellationToken);

        logger.LogInformation("Updated price range for product {ProductId} from new SKU {SkuCode} ({Price} {Currency})",
            msg.ProductId, msg.SkuCode, msg.Price, msg.Currency);
    }
}
