using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.Worker.Consumers;

public sealed class PaymentFailedConsumer(
    IHubContext<NotificationHub> hubContext,
    ILogger<PaymentFailedConsumer> logger) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var evt = context.Message;
        logger.LogWarning("Payment failed: OrderId={OrderId}, Reason={Reason}",
            evt.OrderId, evt.FailureReason);

        // PaymentFailedEvent doesn't carry BuyerId — broadcast to all connected clients.
        // Frontend filters by orderId.
        var message = new OrderUpdateMessage(
            evt.OrderId, string.Empty, "PaymentFailed", evt.FailureReason, DateTime.UtcNow);

        await hubContext.Clients.All
            .SendAsync("OrderUpdate", message, context.CancellationToken);
    }
}
