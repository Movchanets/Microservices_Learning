namespace BuildingBlocks.SharedContracts.Events.Ordering;

public record OrderCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId);
