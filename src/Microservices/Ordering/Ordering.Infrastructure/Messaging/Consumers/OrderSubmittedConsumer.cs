using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Cart;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
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

        var order = Order.Create(evt.BuyerId, address);

        foreach (var item in evt.Items)
        {
            order.AddItem(item.Sku, item.Sku, item.Price, item.Quantity, item.ShopId);
        }

        repository.Add(order);
        await uow.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Order entity created: OrderId={OrderId}", order.Id);
    }
}
