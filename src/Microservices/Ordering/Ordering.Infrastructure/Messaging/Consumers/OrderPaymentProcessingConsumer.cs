using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentProcessingConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderPaymentProcessingConsumer> logger) : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var cmd = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, cmd.OrderId, context.CancellationToken);

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Faulted)
        {
            logger.LogInformation(
                "Skipping PaymentProcessing projection for Order {OrderId} because status is {Status}",
                order.Id, order.Status);
            return;
        }

        if (order.Status == OrderStatus.Submitted)
        {
            // Compensation: OrderInventoryReservedConsumer failed (e.g., race condition).
            // Fast-forward the status so the order doesn't get stuck.
            logger.LogWarning(
                "Order {OrderId} is still Submitted when ProcessPayment arrived — fast-forwarding to InventoryReserved",
                order.Id);
            order.MarkInventoryReserved();

            // Publish the missed InventoryReserved event so the frontend gets the SignalR update
            await publishEndpoint.Publish(new OrderStatusChangedEvent(
                order.Id,
                order.BuyerId,
                OrderStatus.InventoryReserved.ToString(),
                null,
                DateTime.UtcNow), context.CancellationToken);
        }

        if (order.Status == OrderStatus.InventoryReserved)
        {
            order.MarkPaymentProcessing();
            repository.Update(order);
            await uow.SaveChangesAsync(context.CancellationToken);

            await publishEndpoint.Publish(new OrderStatusChangedEvent(
                order.Id,
                order.BuyerId,
                OrderStatus.PaymentProcessing.ToString(),
                null,
                DateTime.UtcNow), context.CancellationToken);
        }
    }
}
