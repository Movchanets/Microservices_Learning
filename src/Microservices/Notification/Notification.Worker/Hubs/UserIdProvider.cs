using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Worker.Hubs;

/// <summary>
/// Maps buyer identity from JWT claims to SignalR's user concept.
/// Prefers authenticated claims over query string for security.
/// </summary>
public sealed class BuyerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return ResolveBuyerId(connection.User, connection.GetHttpContext()?.Request.Query);
    }

    /// <summary>
    /// Extracts buyer ID from claims first, then falls back to query string.
    /// Separated from HubConnectionContext for testability.
    /// </summary>
    internal static string? ResolveBuyerId(
        ClaimsPrincipal? user,
        Microsoft.AspNetCore.Http.IQueryCollection? query)
    {
        var claimId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claimId))
            return claimId;

        // Fallback: query string (for backward compatibility during rollout)
        return query?["buyerId"].ToString();
    }
}
