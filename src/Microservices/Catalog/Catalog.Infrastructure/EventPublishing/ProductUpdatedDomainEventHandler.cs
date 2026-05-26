using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.EventPublishing;

public sealed class ProductUpdatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    ICategoryRepository categoryRepository,
    ILogger<ProductUpdatedDomainEventHandler> logger)
    : INotificationHandler<ProductUpdatedDomainEvent>
{
    public async Task Handle(
        ProductUpdatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(notification.CategoryId, cancellationToken);

        await publishEndpoint.Publish(new ProductUpdatedEvent(
            ProductId: notification.ProductId,
            Name: notification.Name,
            Description: notification.Description,
            CategoryId: notification.CategoryId,
            CategoryName: category?.Name ?? "",
            Tags: notification.Tags,
            ImageUrl: notification.ImageUrl,
            StoreId: notification.StoreId,
            IsActive: notification.IsActive,
            UpdatedAt: notification.UpdatedAt,
            Brand: notification.Brand), cancellationToken);

        logger.LogInformation("Published ProductUpdatedEvent for {ProductId}", notification.ProductId);
    }
}
