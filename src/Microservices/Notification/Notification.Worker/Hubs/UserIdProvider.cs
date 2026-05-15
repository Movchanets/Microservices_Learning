using Microsoft.AspNetCore.SignalR;

namespace Notification.Worker.Hubs;

/// <summary>
/// Maps the x-buyer-id header (set by the BFF gateway) to SignalR's user concept.
/// This enables hubContext.Clients.User(buyerId) to target specific buyers.
/// </summary>
public sealed class BuyerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.GetHttpContext()?.Request.Headers["x-buyer-id"].ToString();
    }
}
