namespace BuildingBlocks.SharedContracts.Events.Ordering;

public record OrderStatusChangedEvent(
    Guid OrderId,
    string BuyerId,
    string NewStatus,
    string? Notes,
    DateTime Timestamp);
