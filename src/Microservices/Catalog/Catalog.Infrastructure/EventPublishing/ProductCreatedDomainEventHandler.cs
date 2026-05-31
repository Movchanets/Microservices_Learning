using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

/// <summary>
/// Handles ProductCreatedDomainEvent and publishes ProductCreatedEvent via MassTransit.
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
            ProductId: notification.ProductId,
            Name: notification.Name,
            Description: notification.Description,
            CategoryId: notification.CategoryId,
            CategoryName: category?.Name ?? "",
            Tags: notification.Tags,
            ImageUrl: notification.ImageUrl,
            StoreId: notification.StoreId,
            CreatedAt: notification.CreatedAt,
            Brand: notification.Brand), cancellationToken);

        logger.LogInformation("Published ProductCreatedEvent for {ProductId}", notification.ProductId);
    }
}
