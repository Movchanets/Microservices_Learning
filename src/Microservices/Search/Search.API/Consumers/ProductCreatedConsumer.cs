using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Models;
using Search.API.Services;

namespace Search.API.Consumers;

public sealed class ProductCreatedConsumer(
    ISearchService searchService,
    ILogger<ProductCreatedConsumer> logger)
    : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
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
            SellerId = msg.SellerId,
            IsActive = true,
            CreatedAt = msg.CreatedAt,
            UpdatedAt = msg.CreatedAt
        };

        await searchService.IndexProductAsync(document, context.CancellationToken);
        logger.LogInformation("Indexed new product {ProductId}: {Name}", msg.ProductId, msg.Name);
    }
}
