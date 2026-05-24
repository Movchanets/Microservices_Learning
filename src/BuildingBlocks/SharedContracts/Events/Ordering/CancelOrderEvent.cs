namespace BuildingBlocks.SharedContracts.Events.Ordering;

/// <summary>
/// Published by the Ordering Saga or API when a buyer requests order cancellation.
/// Consumed by Inventory (release stock) and Payment (refund) as compensation triggers.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order to cancel.</param>
/// <param name="BuyerId">Identity of the buyer requesting cancellation.</param>
/// <param name="Reason">Optional reason for cancellation.</param>
/// <param name="Timestamp">When the cancellation was requested.</param>
public record CancelOrderEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string? Reason,
    DateTime Timestamp);
