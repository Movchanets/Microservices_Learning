using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

/// <summary>
/// Updates the Order entity when an OrderStatusChangedEvent arrives.
/// This consumer handles status transitions that the saga triggers
/// (e.g. PaymentProcessing) without competing with the Payment service
/// for ProcessPaymentCommand.
/// </summary>
public sealed class OrderStatusProjectionConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    ILogger<OrderStatusProjectionConsumer> logger) : IConsumer<OrderStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(
            repository, evt.OrderId, context.CancellationToken);

        var targetStatus = evt.NewStatus;

        // Skip if the order is already at or past the target status
        if (order.Status.ToString() == targetStatus)
        {
            logger.LogDebug(
                "Order {OrderId} already at {Status}, skipping projection",
                order.Id, targetStatus);
            return;
        }

        // Apply the status transition
        switch (targetStatus)
        {
            case nameof(OrderStatus.PaymentProcessing):
                if (order.Status == OrderStatus.Submitted)
                {
                    // Safety net: fast-forward through InventoryReserved
                    logger.LogWarning(
                        "Order {OrderId} is still Submitted — fast-forwarding to InventoryReserved first",
                        order.Id);
                    order.MarkInventoryReserved();
                }
                if (order.Status == OrderStatus.InventoryReserved)
                {
                    order.MarkPaymentProcessing();
                }
                break;

            case nameof(OrderStatus.InventoryReserved):
                if (order.Status == OrderStatus.Submitted)
                {
                    order.MarkInventoryReserved();
                }
                break;

            case nameof(OrderStatus.Completed):
                if (order.Status is not (OrderStatus.Cancelled or OrderStatus.Faulted))
                {
                    order.MarkCompleted();
                }
                break;

            case nameof(OrderStatus.Cancelled):
                order.MarkCancelled(evt.Notes ?? "Cancelled");
                break;

            case nameof(OrderStatus.Faulted):
                order.MarkFaulted(evt.Notes ?? "Faulted");
                break;

            default:
                logger.LogWarning(
                    "Unknown target status {Status} for Order {OrderId}",
                    targetStatus, order.Id);
                return;
        }

        repository.Update(order);
        await uow.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Order {OrderId} projected to {Status}",
            order.Id, order.Status);
    }
}
