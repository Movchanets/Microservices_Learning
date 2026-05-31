using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Models;
using Search.API.Services;

namespace Search.API.Consumers;

/// <summary>
/// Handles ProductCreatedEvent from Catalog.
/// Indexes the new product in Elasticsearch.
/// Price/SKU data arrives later via SkuCreatedIntegrationEvent.
/// </summary>
public sealed class ProductCreatedConsumer(
    ISearchService searchService,
    ILogger<ProductCreatedConsumer> logger)
    : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing ProductCreatedEvent for {ProductId}: {Name}",
            msg.ProductId, msg.Name);

        var document = new ProductSearchDocument
        {
            Id = msg.ProductId,
            Name = msg.Name,
            Description = msg.Description,
            CategoryId = msg.CategoryId,
            CategoryName = msg.CategoryName,
            Tags = msg.Tags,
            ImageUrl = msg.ImageUrl,
            StoreId = msg.StoreId,
            IsActive = true,
            CreatedAt = msg.CreatedAt,
            UpdatedAt = msg.CreatedAt,
            Brand = msg.Brand,
            Attributes = msg.Attributes ?? [],
            // Price/SKU fields start at zero — updated when SkuCreatedEvent arrives
            MinPrice = 0,
            MaxPrice = 0,
            Currency = "USD",
            SkuCount = 0,
        };

        await searchService.IndexProductAsync(document, context.CancellationToken);

        logger.LogInformation("Indexed product {ProductId} in search (no SKUs yet)", msg.ProductId);
    }
}
