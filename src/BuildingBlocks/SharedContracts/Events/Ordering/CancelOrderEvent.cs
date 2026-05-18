namespace BuildingBlocks.SharedContracts.Events.Ordering;

public record CancelOrderEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string? Reason,
    DateTime Timestamp);
