using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

public sealed class ProductDeletedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<ProductDeletedDomainEventHandler> logger)
    : INotificationHandler<ProductDeletedDomainEvent>
{
    public async Task Handle(
        ProductDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new ProductDeletedEvent(
            notification.ProductId,
            DateTime.UtcNow), cancellationToken);

        logger.LogInformation("Published ProductDeletedEvent for {ProductId}", notification.ProductId);
    }
}
