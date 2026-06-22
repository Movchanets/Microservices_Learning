using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

/// <summary>
/// Updates the Order entity when OrderCompletedEvent arrives from the saga.
/// Sets order status to Completed and publishes domain event for Notification service.
/// </summary>
public sealed class OrderCompletedProjectionConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint) : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, evt.OrderId, context.CancellationToken);

        await OrderConsumerHelpers.FastForwardAndPublishAsync(
            repository, uow, publishEndpoint, order, OrderStatus.Completed, null, context.CancellationToken);
    }
}
