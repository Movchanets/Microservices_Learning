namespace BuildingBlocks.SharedContracts.Events.Payment;

/// <summary>
/// Published by Payment Service after successfully processing a refund.
/// Consumed by the Ordering Saga to confirm compensation completed.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order whose payment was refunded.</param>
/// <param name="TransactionId">Original payment transaction identifier.</param>
/// <param name="RefundId">Unique identifier for this refund.</param>
/// <param name="Amount">Amount refunded.</param>
/// <param name="Reason">Human-readable reason for the refund.</param>
public record PaymentRefundedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,
    Guid RefundId,
    decimal Amount,
    string Reason);
