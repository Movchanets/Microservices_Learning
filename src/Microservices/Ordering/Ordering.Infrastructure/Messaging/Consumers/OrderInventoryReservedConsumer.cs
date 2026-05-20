using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Inventory;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class OrderInventoryReservedConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderInventoryReservedConsumer> logger) : IConsumer<InventoryReservedEvent>
{
    public async Task Consume(ConsumeContext<InventoryReservedEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, evt.OrderId, context.CancellationToken);

        if (order.Status != OrderStatus.Submitted)
        {
            logger.LogInformation(
                "Skipping InventoryReserved projection for Order {OrderId} because status is {Status}",
                order.Id, order.Status);
            return;
        }

        order.MarkInventoryReserved();
        repository.Update(order);
        await uow.SaveChangesAsync(context.CancellationToken);

        await publishEndpoint.Publish(new OrderStatusChangedEvent(
            order.Id,
            order.BuyerId,
            OrderStatus.InventoryReserved.ToString(),
            null,
            DateTime.UtcNow), context.CancellationToken);
    }
}
