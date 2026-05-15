namespace BuildingBlocks.SharedContracts.Events.Ordering;

public record OrderCancelledEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string Reason);
