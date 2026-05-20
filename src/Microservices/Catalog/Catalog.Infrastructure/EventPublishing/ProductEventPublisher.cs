using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Domain.Aggregates;
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
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
        if (product is null) return;

        var category = await categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken);

        await publishEndpoint.Publish(new ProductCreatedEvent(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.CategoryId,
            category?.Name ?? "",
            product.Tags,
            product.ImageUrl,
            product.StoreId,
            product.CreatedAt), cancellationToken);

        logger.LogInformation("Published ProductCreatedEvent for {ProductId}", notification.ProductId);
    }
}

public sealed class ProductUpdatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ILogger<ProductUpdatedDomainEventHandler> logger)
    : INotificationHandler<ProductUpdatedDomainEvent>
{
    public async Task Handle(
        ProductUpdatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
        if (product is null) return;

        var category = await categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken);

        await publishEndpoint.Publish(new ProductUpdatedEvent(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.CategoryId,
            category?.Name ?? "",
            product.Tags,
            product.ImageUrl,
            product.StoreId,
            product.IsActive,
            product.UpdatedAt ?? DateTime.UtcNow), cancellationToken);

        logger.LogInformation("Published ProductUpdatedEvent for {ProductId}", notification.ProductId);
    }
}

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
