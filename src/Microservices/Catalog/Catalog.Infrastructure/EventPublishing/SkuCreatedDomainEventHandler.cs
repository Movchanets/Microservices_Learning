using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

/// <summary>
/// Handles SkuCreatedDomainEvent and publishes SkuCreatedIntegrationEvent via MassTransit.
/// Consumed by Inventory.API to create an InventoryItem for the new SKU.
/// </summary>
public sealed class SkuCreatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<SkuCreatedDomainEventHandler> logger)
    : INotificationHandler<SkuCreatedDomainEvent>
{
    public async Task Handle(
        SkuCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new SkuCreatedIntegrationEvent(
            ProductId: notification.ProductId,
            SkuId: notification.SkuId,
            SkuCode: notification.SkuCode,
            ProductName: notification.ProductName,
            StoreId: notification.StoreId,
            Price: notification.Price,
            Currency: notification.Currency,
            TypedAttributes: notification.TypedAttributes,
            FlexibleAttributes: notification.FlexibleAttributes,
            Timestamp: DateTime.UtcNow), cancellationToken);

        logger.LogInformation(
            "Published SkuCreatedIntegrationEvent for SKU {SkuCode} on Product {ProductId}",
            notification.SkuCode, notification.ProductId);
    }
}
