namespace BuildingBlocks.SharedContracts.Events.Ordering;

/// <summary>
/// Published by the Ordering Saga after all compensation steps complete for a cancelled order.
/// Consumed by Notification Service to send cancellation confirmation to the buyer.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The cancelled order.</param>
/// <param name="BuyerId">Identity of the buyer.</param>
/// <param name="Reason">Reason for the cancellation.</param>
/// <param name="Timestamp">When the cancellation was finalized.</param>
public record OrderCancelledEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string Reason,
    DateTime Timestamp = default);
