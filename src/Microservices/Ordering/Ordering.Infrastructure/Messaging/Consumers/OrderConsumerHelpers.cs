using Ordering.Domain.Aggregates;

namespace Ordering.Infrastructure.Messaging.Consumers;

internal static class OrderConsumerHelpers
{
    public static async Task<Order> LoadOrderOrThrowAsync(
        IOrderRepository repository,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var order = await repository.GetByIdAsync(orderId, cancellationToken);
            if (order is not null)
            {
                return order;
            }

            if (attempt < 4)
            {
                await Task.Delay(200, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Order {orderId} was not found after retries. Postponing message consumption.");
    }
}
