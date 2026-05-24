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
    IPublishEndpoint publishEndpoint,
    ILogger<OrderCancelledProjectionConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, evt.OrderId, context.CancellationToken);

        if (order.Status == OrderStatus.Completed)
        {
            logger.LogWarning("Ignoring cancellation for completed Order {OrderId}", evt.OrderId);
            return;
        }

        await OrderConsumerHelpers.FastForwardAndPublishAsync(
            repository, uow, publishEndpoint, order, OrderStatus.Cancelled, evt.Reason, context.CancellationToken);
    }
}
