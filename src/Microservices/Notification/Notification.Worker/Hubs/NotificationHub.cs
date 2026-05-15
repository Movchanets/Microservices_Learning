using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Notification.Worker.Hubs;

/// <summary>
/// SignalR Hub for real-time order notifications.
/// Clients connect to /hubs/notifications and receive OrderUpdate messages.
/// All message sending is done via IHubContext from MassTransit consumers.
/// </summary>
public sealed class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        var buyerId = Context.GetHttpContext()?.Request.Headers["x-buyer-id"].ToString();
        logger.LogInformation("Client connected: ConnectionId={ConnectionId}, BuyerId={BuyerId}",
            Context.ConnectionId, buyerId ?? "anonymous");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: ConnectionId={ConnectionId}, Error={Error}",
            Context.ConnectionId, exception?.Message ?? "none");
        return base.OnDisconnectedAsync(exception);
    }
}
