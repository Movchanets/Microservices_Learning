using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

public sealed class ProductPriceChangedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<ProductPriceChangedDomainEventHandler> logger)
    : INotificationHandler<ProductPriceChangedDomainEvent>
{
    public async Task Handle(
        ProductPriceChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new ProductPriceChangedEvent(
            notification.ProductId,
            notification.OldPrice,
            notification.NewPrice,
            notification.Currency,
            DateTime.UtcNow), cancellationToken);

        logger.LogInformation("Published ProductPriceChangedEvent for {ProductId}: {OldPrice} → {NewPrice}",
            notification.ProductId, notification.OldPrice, notification.NewPrice);
    }
}
