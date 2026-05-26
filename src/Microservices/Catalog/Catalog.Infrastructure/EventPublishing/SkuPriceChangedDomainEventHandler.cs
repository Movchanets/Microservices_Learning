using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

/// <summary>
/// Handles SkuPriceChangedDomainEvent and publishes SkuPriceChangedEvent via MassTransit.
/// Consumed by Cart.API to update the cached product price.
/// Consumed by Search.API to update the price in the search index.
/// </summary>
public sealed class SkuPriceChangedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<SkuPriceChangedDomainEventHandler> logger)
    : INotificationHandler<SkuPriceChangedDomainEvent>
{
    public async Task Handle(
        SkuPriceChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new SkuPriceChangedEvent(
            ProductId: notification.ProductId,
            SkuId: notification.SkuId,
            SkuCode: notification.SkuCode,
            OldPrice: notification.OldPrice,
            NewPrice: notification.NewPrice,
            Currency: notification.Currency,
            ChangedAt: DateTime.UtcNow), cancellationToken);

        logger.LogInformation(
            "Published SkuPriceChangedEvent for SKU {SkuCode}: {OldPrice} -> {NewPrice} {Currency}",
            notification.SkuCode, notification.OldPrice, notification.NewPrice, notification.Currency);
    }
}
