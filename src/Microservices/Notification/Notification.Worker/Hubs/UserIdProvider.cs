using Microsoft.AspNetCore.SignalR;

namespace Notification.Worker.Hubs;

/// <summary>
/// Maps buyer identity from the SignalR handshake to SignalR's user concept.
/// Browser WebSocket clients cannot reliably send custom headers, so query string is primary.
/// </summary>
public sealed class BuyerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var httpContext = connection.GetHttpContext();
        var buyerId = httpContext?.Request.Query["buyerId"].ToString();

        if (!string.IsNullOrWhiteSpace(buyerId))
        {
            return buyerId;
        }

        return httpContext?.Request.Headers["x-buyer-id"].ToString();
    }
}
