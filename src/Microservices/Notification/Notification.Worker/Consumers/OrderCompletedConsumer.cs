using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.Worker.Consumers;

public sealed class OrderCompletedConsumer(
    IHubContext<NotificationHub> hubContext,
    ILogger<OrderCompletedConsumer> logger) : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Order completed: OrderId={OrderId}, BuyerId={BuyerId}",
            evt.OrderId, evt.BuyerId);

        var message = new OrderUpdateMessage(
            evt.OrderId, evt.BuyerId, "Completed", null, DateTime.UtcNow);

        await hubContext.Clients.User(evt.BuyerId)
            .SendAsync("OrderUpdate", message, context.CancellationToken);
    }
}
