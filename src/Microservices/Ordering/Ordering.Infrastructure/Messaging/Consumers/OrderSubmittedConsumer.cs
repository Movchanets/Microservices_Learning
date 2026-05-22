using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Cart;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;
using Ordering.Domain.ValueObjects;

namespace Ordering.Infrastructure.Messaging.Consumers;

/// <summary>
/// Creates the Order domain entity when OrderSubmittedEvent arrives.
/// The saga handles orchestration; this consumer handles persistence.
/// Idempotent — skips if order already exists.
/// </summary>
public sealed class OrderSubmittedConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderSubmittedConsumer> logger) : IConsumer<OrderSubmittedEvent>
{
    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Creating order entity: OrderId={OrderId}, BuyerId={BuyerId}",
            evt.CorrelationId, evt.BuyerId);

        // Idempotency — saga may replay events
        var existing = await repository.GetByIdAsync(evt.CorrelationId, context.CancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Order {OrderId} already exists, skipping", evt.CorrelationId);
            return;
        }

        var address = Address.FromShipping(
            evt.ShippingAddressLine1, evt.ShippingAddressLine2,
            evt.ShippingCity, evt.ShippingState,
            evt.ShippingPostalCode, evt.ShippingCountry);

        var order = Order.Create(evt.BuyerId, address, orderId: evt.CorrelationId);

        foreach (var item in evt.Items)
        {
            order.AddItem(item.ProductId, item.ProductId.ToString(), item.Price, item.Quantity, item.StoreId);
        }

        repository.Add(order);
        await uow.SaveChangesAsync(context.CancellationToken);

        // Publish Submitted status so SignalR notifies the frontend immediately.
        // Without this, the frontend has no real-time update until InventoryReserved.
        await publishEndpoint.Publish(new OrderStatusChangedEvent(
            order.Id,
            order.BuyerId,
            OrderStatus.Submitted.ToString(),
            null,
            DateTime.UtcNow), context.CancellationToken);

        logger.LogInformation("Order entity created: OrderId={OrderId}", order.Id);
    }
}
