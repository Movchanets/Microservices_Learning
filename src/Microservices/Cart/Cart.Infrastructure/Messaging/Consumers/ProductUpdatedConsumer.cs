using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

public sealed class ProductUpdatedConsumer(
    IProductPriceRepository priceRepository,
    ILogger<ProductUpdatedConsumer> logger) : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Product updated: {ProductId}, SKU={Sku}, Price={Price}, StoreId={StoreId}",
            evt.ProductId, evt.Sku, evt.Price, evt.StoreId);

        await priceRepository.UpsertAsync(
            evt.ProductId, evt.Sku, evt.Name, evt.Price, evt.Currency, evt.StoreId, context.CancellationToken);
    }
}
