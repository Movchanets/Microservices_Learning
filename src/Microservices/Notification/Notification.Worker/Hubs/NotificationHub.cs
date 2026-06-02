using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Notification.Worker.Hubs;

/// <summary>
/// SignalR hub for real-time notifications to buyers.
/// Authenticates via JWT cookie from the Gateway. Clients join a group
/// keyed by their BuyerId for targeted message delivery.
/// </summary>
[Authorize]
public sealed class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        var buyerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
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
