using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Models;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles ProductUpdatedEvent from Catalog.
/// Updates product metadata (name, description, category, etc.) in Elasticsearch.
/// Price/SKU data is handled separately by SkuCreated/SkuPriceChanged consumers.
/// </summary>
public sealed class ProductUpdatedConsumer(
    ISearchService searchService,
    ILogger<ProductUpdatedConsumer> logger)
    : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing ProductUpdatedEvent for {ProductId}: {Name}",
            msg.ProductId, msg.Name);

        var request = new UpdateProductMetadataRequest(
            msg.ProductId,
            msg.Name,
            msg.Description,
            msg.CategoryId,
            msg.CategoryName,
            msg.Tags,
            msg.ImageUrl,
            msg.StoreId,
            msg.IsActive,
            msg.UpdatedAt,
            msg.Brand,
            msg.Attributes);

        await searchService.UpdateProductMetadataAsync(request, context.CancellationToken);

        logger.LogInformation("Updated product {ProductId} metadata in search index", msg.ProductId);
    }
}
