using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

/// <summary>
/// Handles domain events and publishes corresponding integration events via MassTransit.
/// The Outbox pattern ensures these are published atomically with the DB transaction.
/// </summary>
public sealed class ProductCreatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ICategoryRepository categoryRepository,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(notification.CategoryId, cancellationToken);

        await publishEndpoint.Publish(new ProductCreatedEvent(
            notification.ProductId,
            notification.Name,
            notification.Description,
            notification.Price,
            notification.Currency,
            notification.Sku,
            notification.CategoryId,
            category?.Name ?? "",
            notification.Tags,
            notification.ImageUrl,
            notification.StoreId,
            notification.CreatedAt), cancellationToken);

        logger.LogInformation("Published ProductCreatedEvent for {ProductId}", notification.ProductId);
    }
}
