using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class OrderCompletedProjectionConsumer(
    IOrderRepository repository,
    IUnitOfWork uow) : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, evt.OrderId, context.CancellationToken);

        if (order.Status == OrderStatus.Completed)
        {
            return;
        }

        if (order.Status == OrderStatus.Submitted)
        {
            order.MarkInventoryReserved();
        }

        if (order.Status == OrderStatus.InventoryReserved)
        {
            order.MarkPaymentProcessing();
        }

        if (order.Status == OrderStatus.PaymentProcessing)
        {
            order.MarkCompleted();
            repository.Update(order);
            await uow.SaveChangesAsync(context.CancellationToken);
        }
    }
}
