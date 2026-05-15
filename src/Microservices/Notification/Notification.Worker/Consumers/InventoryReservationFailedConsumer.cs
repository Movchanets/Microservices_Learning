using BuildingBlocks.SharedContracts.Events.Inventory;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.Worker.Consumers;

public sealed class InventoryReservationFailedConsumer(
    IHubContext<NotificationHub> hubContext,
    ILogger<InventoryReservationFailedConsumer> logger) : IConsumer<InventoryReservationFailedEvent>
{
    public async Task Consume(ConsumeContext<InventoryReservationFailedEvent> context)
    {
        var evt = context.Message;
        logger.LogWarning("Inventory reservation failed: OrderId={OrderId}, Reason={Reason}",
            evt.OrderId, evt.Reason);

        var message = new OrderUpdateMessage(
            evt.OrderId, string.Empty, "InventoryFailed", evt.Reason, DateTime.UtcNow);

        await hubContext.Clients.All
            .SendAsync("OrderUpdate", message, context.CancellationToken);
    }
}
