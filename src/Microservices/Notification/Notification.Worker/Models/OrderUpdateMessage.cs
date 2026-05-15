namespace Notification.Worker.Models;

public sealed record OrderUpdateMessage(
    Guid OrderId,
    string BuyerId,
    string Status,
    string? Reason,
    DateTime Timestamp);
