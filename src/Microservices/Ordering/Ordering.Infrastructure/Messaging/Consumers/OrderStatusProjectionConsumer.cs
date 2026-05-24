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
    IPublishEndpoint publishEndpoint,
    ILogger<OrderStatusProjectionConsumer> logger) : IConsumer<OrderStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var evt = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(
            repository, evt.OrderId, context.CancellationToken);

        if (!Enum.TryParse<OrderStatus>(evt.NewStatus, ignoreCase: true, out var targetStatus))
        {
            logger.LogWarning("Unknown target status {Status} for Order {OrderId}", evt.NewStatus, order.Id);
            return;
        }

        // Skip if already at target
        if (order.Status == targetStatus)
        {
            logger.LogDebug("Order {OrderId} already at {Status}, skipping projection", order.Id, targetStatus);
            return;
        }

        var updated = await OrderConsumerHelpers.FastForwardAndPublishAsync(
            repository, uow, publishEndpoint, order, targetStatus, evt.Notes, context.CancellationToken);

        if (updated)
            logger.LogInformation("Order {OrderId} projected to {Status}", order.Id, order.Status);
        else
            logger.LogWarning("Order {OrderId} could not reach {Status} from {CurrentStatus}", order.Id, targetStatus, order.Status);
    }
}
