namespace BuildingBlocks.SharedContracts.Events.Payment;

/// <summary>
/// Published by Payment Service after successfully charging the buyer.
/// Consumed by the Ordering Saga to advance to order completion.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order that was paid for.</param>
/// <param name="TransactionId">Payment gateway transaction identifier.</param>
public record PaymentCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string TransactionId);
