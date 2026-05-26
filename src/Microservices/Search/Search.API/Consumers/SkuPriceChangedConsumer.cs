using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles SkuPriceChangedEvent from Catalog.
/// Partially updates the price field in the search index for the affected product.
/// </summary>
public sealed class SkuPriceChangedConsumer(
    ISearchService searchService,
    ILogger<SkuPriceChangedConsumer> logger)
    : IConsumer<SkuPriceChangedEvent>
{
    public async Task Consume(ConsumeContext<SkuPriceChangedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuPriceChangedEvent for SKU {SkuCode}: {OldPrice} -> {NewPrice}",
            msg.SkuCode, msg.OldPrice, msg.NewPrice);

        await searchService.UpdateProductPriceAsync(
            msg.ProductId, msg.NewPrice, msg.Currency, context.CancellationToken);

        logger.LogInformation("Updated price for product {ProductId} in search index ({NewPrice} {Currency})",
            msg.ProductId, msg.NewPrice, msg.Currency);
    }
}
