using Ordering.Domain.Aggregates;

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
}
