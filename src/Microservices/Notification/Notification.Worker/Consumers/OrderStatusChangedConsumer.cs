using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.Worker.Consumers;

/// <summary>
/// Consumes OrderStatusChangedEvent and pushes real-time updates to the buyer
/// via SignalR. Sends to the buyer's group keyed by BuyerId.
/// </summary>
public sealed class OrderStatusChangedConsumer(
    IHubContext<NotificationHub> hubContext,
    ILogger<OrderStatusChangedConsumer> logger) : IConsumer<OrderStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Order status changed: OrderId={OrderId}, BuyerId={BuyerId}, NewStatus={Status}",
            evt.OrderId, evt.BuyerId, evt.NewStatus);

        var message = new OrderUpdateMessage(
            evt.OrderId, evt.BuyerId, evt.NewStatus, evt.Notes, evt.Timestamp);

        await hubContext.Clients.User(evt.BuyerId)
            .SendAsync("OrderUpdate", message, context.CancellationToken);
    }
}
