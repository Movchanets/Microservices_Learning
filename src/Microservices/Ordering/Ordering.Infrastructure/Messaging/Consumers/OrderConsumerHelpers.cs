using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

internal static class OrderConsumerHelpers
{
    public static async Task<Order> LoadOrderOrThrowAsync(
        IOrderRepository repository,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        // 10 attempts × 500ms = 5 seconds total window.
        // The OrderSubmittedConsumer may race with saga-driven consumers
        // (InventoryReserved, ProcessPayment) — the order entity must exist
        // before projection consumers can update it.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var order = await repository.GetByIdAsync(orderId, cancellationToken);
            if (order is not null)
            {
                return order;
            }

            if (attempt < 9)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Order {orderId} was not found after retries. Postponing message consumption.");
    }

    /// <summary>
    /// Publishes an OrderStatusChangedEvent for SignalR notification.
    /// Centralizes the event publishing pattern used by all projection consumers.
    /// </summary>
    public static async Task PublishStatusChangedAsync(
        IPublishEndpoint publishEndpoint,
        Order order,
        string? notes,
        CancellationToken ct)
    {
        await publishEndpoint.Publish(new OrderStatusChangedEvent(
            order.Id,
            order.BuyerId,
            order.Status.ToString(),
            notes,
            DateTime.UtcNow), ct);
    }

    /// <summary>
    /// Fast-forwards an order to the target status, persists, and publishes
    /// the status change for SignalR. Combines the common pattern of:
    /// load → fast-forward → save → publish.
    /// Returns true if the order was updated, false if already at/past target.
    /// </summary>
    public static async Task<bool> FastForwardAndPublishAsync(
        IOrderRepository repository,
        IUnitOfWork uow,
        IPublishEndpoint publishEndpoint,
        Order order,
        OrderStatus targetStatus,
        string? notes,
        CancellationToken ct)
    {
        if (!order.FastForwardTo(targetStatus, notes))
            return false;

        repository.Update(order);
        await uow.SaveChangesAsync(ct);
        await PublishStatusChangedAsync(publishEndpoint, order, notes, ct);
        return true;
    }
}
