using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductCreatedConsumer(
    IProductPriceRepository priceRepository,
    ILogger<ProductCreatedConsumer> logger) : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product created: {ProductId}, SKU={Sku}, Price={Price}",
            evt.ProductId, evt.Sku, evt.Price);

        await priceRepository.UpsertAsync(
            evt.ProductId, evt.Sku, evt.Name, evt.Price, evt.Currency, context.CancellationToken);
    }
}
