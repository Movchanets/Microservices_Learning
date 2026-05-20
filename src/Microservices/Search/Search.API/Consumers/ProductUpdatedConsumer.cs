using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Models;
using Search.API.Services;

namespace Search.API.Consumers;

public sealed class ProductUpdatedConsumer(
    ISearchService searchService,
    ILogger<ProductUpdatedConsumer> logger)
    : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var msg = context.Message;

        var document = new ProductSearchDocument
        {
            Id = msg.ProductId,
            Name = msg.Name,
            Description = msg.Description,
            Price = msg.Price,
            Currency = msg.Currency,
            Sku = msg.Sku,
            CategoryId = msg.CategoryId,
            CategoryName = msg.CategoryName,
            Tags = msg.Tags,
            ImageUrl = msg.ImageUrl,
            StoreId = msg.StoreId,
            IsActive = msg.IsActive,
            UpdatedAt = msg.UpdatedAt,
            Brand = msg.Brand,
            Attributes = msg.Attributes ?? []
        };

        await searchService.UpdateProductAsync(document, context.CancellationToken);
        logger.LogInformation("Updated product {ProductId} in search index", msg.ProductId);
    }
}
