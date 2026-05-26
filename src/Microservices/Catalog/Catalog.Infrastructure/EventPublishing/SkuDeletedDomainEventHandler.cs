using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

/// <summary>
/// Handles SkuDeletedDomainEvent and publishes SkuDeletedEvent via MassTransit.
/// Consumed by Inventory.API to deactivate the corresponding InventoryItem.
/// Consumed by Search.API to remove the SKU from the search index.
/// </summary>
public sealed class SkuDeletedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<SkuDeletedDomainEventHandler> logger)
    : INotificationHandler<SkuDeletedDomainEvent>
{
    public async Task Handle(
        SkuDeletedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new SkuDeletedEvent(
            ProductId: notification.ProductId,
            SkuId: notification.SkuId,
            SkuCode: notification.SkuCode,
            DeletedAt: DateTime.UtcNow), cancellationToken);

        logger.LogInformation(
            "Published SkuDeletedEvent for SKU {SkuCode} (SkuId={SkuId}) on Product {ProductId}",
            notification.SkuCode, notification.SkuId, notification.ProductId);
    }
}
