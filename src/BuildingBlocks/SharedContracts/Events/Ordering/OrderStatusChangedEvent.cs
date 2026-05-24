namespace BuildingBlocks.SharedContracts.Events.Ordering;

/// <summary>
/// Published by the Ordering Service when an order's status changes
/// (e.g. Pending → Confirmed → Shipped → Delivered).
/// Consumed by Notification (push updates) and StoreManagement (seller dashboard).
/// </summary>
/// <param name="OrderId">The order whose status changed.</param>
/// <param name="BuyerId">Identity of the buyer.</param>
/// <param name="NewStatus">The new order status.</param>
/// <param name="Notes">Optional notes about the status change.</param>
/// <param name="Timestamp">When the status changed.</param>
public record OrderStatusChangedEvent(
    Guid OrderId,
    string BuyerId,
    string NewStatus,
    string? Notes,
    DateTime Timestamp);
