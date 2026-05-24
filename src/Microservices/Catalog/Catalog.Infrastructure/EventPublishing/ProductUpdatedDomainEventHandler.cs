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
            notification.IsActive,
            notification.UpdatedAt), cancellationToken);

        logger.LogInformation("Published ProductUpdatedEvent for {ProductId}", notification.ProductId);
    }
}
