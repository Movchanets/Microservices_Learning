using BuildingBlocks.SharedContracts.Events.Catalog;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SkuCreatedIntegrationEvent from Catalog.
/// Creates a ProductPrice entry for the new SKU so it can be added to carts.
/// </summary>
public sealed class SkuCreatedConsumer(
    IProductPriceRepository priceRepository,
    ILogger<SkuCreatedConsumer> logger) : IConsumer<SkuCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SkuCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing SkuCreatedEvent for SKU {SkuCode} (SkuId={SkuId}) on Product {ProductId}",
            msg.SkuCode, msg.SkuId, msg.ProductId);

        await priceRepository.UpsertAsync(
            msg.ProductId, msg.SkuId, msg.SkuCode, msg.ProductName,
            msg.Price, msg.Currency, msg.StoreId, context.CancellationToken);

        logger.LogInformation("Created/updated ProductPrice for SKU {SkuCode} ({Price} {Currency})",
            msg.SkuCode, msg.Price, msg.Currency);
    }
}
