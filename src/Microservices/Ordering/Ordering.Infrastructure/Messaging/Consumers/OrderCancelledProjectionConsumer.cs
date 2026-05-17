using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class OrderCancelledProjectionConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    ILogger<OrderCancelledProjectionConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderAsync(repository, evt.OrderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found while applying OrderCancelledEvent", evt.OrderId);
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        if (order.Status == OrderStatus.Completed)
        {
            logger.LogWarning("Ignoring cancellation for completed Order {OrderId}", evt.OrderId);
            return;
        }

        order.MarkCancelled(evt.Reason);
        repository.Update(order);
        await uow.SaveChangesAsync(context.CancellationToken);
    }
}
