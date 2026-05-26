using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles SkuDeletedEvent from Catalog.
/// Decrements SKU count. Price recalculation requires re-querying Catalog
/// (or a full reindex) — for now we just decrement the count.
/// Product-level deletion is handled by ProductDeletedConsumer.
/// </summary>
public sealed class SkuDeletedConsumer(
    ISearchService searchService,
    ILogger<SkuDeletedConsumer> logger)
    : IConsumer<SkuDeletedEvent>
{
    public async Task Consume(ConsumeContext<SkuDeletedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuDeletedEvent for SKU {SkuCode} on Product {ProductId}",
            msg.SkuCode, msg.ProductId);

        await searchService.RemoveSkuFromProductAsync(
            msg.ProductId, context.CancellationToken);

        logger.LogInformation("Decremented SKU count for product {ProductId} after removing SKU {SkuCode}",
            msg.ProductId, msg.SkuCode);
    }
}
