namespace BuildingBlocks.SharedContracts.Events.Payment;

public record PaymentRefundedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,
    Guid RefundId,
    decimal Amount,
    string Reason);
