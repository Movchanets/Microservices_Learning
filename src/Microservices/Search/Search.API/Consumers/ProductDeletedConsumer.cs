using BuildingBlocks.SharedContracts.Events.Catalog;
using MassTransit;
using Search.API.Services;

namespace Search.API.Consumers;

public sealed class ProductDeletedConsumer(
    ISearchService searchService,
    ILogger<ProductDeletedConsumer> logger)
    : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var msg = context.Message;

        await searchService.DeleteProductAsync(msg.ProductId, context.CancellationToken);
        logger.LogInformation("Removed product {ProductId} from search index", msg.ProductId);
    }
}
