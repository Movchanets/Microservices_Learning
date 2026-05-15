using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.Worker.Consumers;

public sealed class OrderCancelledConsumer(
    IHubContext<NotificationHub> hubContext,
    ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Order cancelled: OrderId={OrderId}, BuyerId={BuyerId}, Reason={Reason}",
            evt.OrderId, evt.BuyerId, evt.Reason);

        var message = new OrderUpdateMessage(
            evt.OrderId, evt.BuyerId, "Cancelled", evt.Reason, DateTime.UtcNow);

        await hubContext.Clients.User(evt.BuyerId)
            .SendAsync("OrderUpdate", message, context.CancellationToken);
    }
}
